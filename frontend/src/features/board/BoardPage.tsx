import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from 'react';
import {
  DndContext,
  type DragEndEvent,
  PointerSensor,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import type { ApiError } from '../../api/client';
import { type Epic, epicsApi } from '../../api/epics';
import { type Team, teamsApi } from '../../api/teams';
import {
  TICKET_STATES,
  TICKET_TYPES,
  type Ticket,
  type TicketState,
  type TicketType,
  stateLabel,
  ticketsApi,
} from '../../api/tickets';
import { Button } from '../../components/Button';
import { stateColors, typeColors } from '../../components/theme';
import { TicketDetails } from '../tickets/TicketDetails';
import { TicketForm } from '../tickets/TicketForm';

type Overlay = { kind: 'none' } | { kind: 'create' } | { kind: 'details'; ticket: Ticket } | { kind: 'edit'; ticket: Ticket };

export function BoardPage() {
  const [teams, setTeams] = useState<Team[]>([]);
  const [teamId, setTeamId] = useState('');
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [epics, setEpics] = useState<Epic[]>([]);
  const [error, setError] = useState<string | null>(null);

  const [typeFilter, setTypeFilter] = useState<TicketType | ''>('');
  const [epicFilter, setEpicFilter] = useState('');
  const [search, setSearch] = useState('');
  const [overlay, setOverlay] = useState<Overlay>({ kind: 'none' });

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  useEffect(() => {
    teamsApi
      .list()
      .then(setTeams)
      .catch((err: ApiError) => setError(err.title));
  }, []);

  const loadBoard = (id: string) => {
    if (!id) {
      setTickets([]);
      setEpics([]);
      return;
    }
    setError(null);
    ticketsApi
      .list(id)
      .then(setTickets)
      .catch((err: ApiError) => setError(err.title));
    epicsApi
      .list(id)
      .then(setEpics)
      .catch(() => setEpics([]));
  };

  useEffect(() => loadBoard(teamId), [teamId]);

  const epicTitle = (epicId: string | null) =>
    epicId ? (epics.find((e) => e.id === epicId)?.title ?? '—') : '—';

  // Filters combine with AND, applied client-side.
  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return tickets.filter(
      (t) =>
        (!typeFilter || t.type === typeFilter) &&
        (!epicFilter || t.epicId === epicFilter) &&
        (!term || t.title.toLowerCase().includes(term)),
    );
  }, [tickets, typeFilter, epicFilter, search]);

  const columns = useMemo(() => {
    const byState = new Map<TicketState, Ticket[]>();
    for (const state of TICKET_STATES) {
      byState.set(
        state,
        filtered
          .filter((t) => t.state === state)
          .sort((a, b) => b.modifiedAt.localeCompare(a.modifiedAt)),
      );
    }
    return byState;
  }, [filtered]);

  const onDragEnd = async (event: DragEndEvent) => {
    const ticketId = String(event.active.id);
    const targetState = event.over ? (String(event.over.id) as TicketState) : null;
    const ticket = tickets.find((t) => t.id === ticketId);
    if (!ticket || !targetState || ticket.state === targetState) {
      return;
    }

    const previousState = ticket.state;
    // Optimistic move.
    setTickets((prev) => prev.map((t) => (t.id === ticketId ? { ...t, state: targetState } : t)));
    try {
      await ticketsApi.changeState(ticketId, targetState);
      loadBoard(teamId); // refresh ordering (modified_at)
    } catch (err) {
      // Revert on failure and surface the error.
      setTickets((prev) => prev.map((t) => (t.id === ticketId ? { ...t, state: previousState } : t)));
      setError(`Could not move ticket: ${(err as ApiError).title}`);
    }
  };

  const closeOverlay = () => {
    setOverlay({ kind: 'none' });
    loadBoard(teamId);
  };

  const deleteTicket = async (id: string) => {
    try {
      await ticketsApi.remove(id);
      closeOverlay();
    } catch (err) {
      setError((err as ApiError).title);
    }
  };

  return (
    <div style={{ padding: '1.5rem' }}>
      <h1 style={{ margin: '0 0 1rem' }}>Board</h1>

      <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'center', marginBottom: '1rem' }}>
        <select style={controlStyle} value={teamId} onChange={(e) => setTeamId(e.target.value)}>
          <option value="">— Select a team —</option>
          {teams.map((team) => (
            <option key={team.id} value={team.id}>
              {team.name}
            </option>
          ))}
        </select>

        {teamId && (
          <>
            <select style={controlStyle} value={typeFilter} onChange={(e) => setTypeFilter(e.target.value as TicketType | '')}>
              <option value="">All types</option>
              {TICKET_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
            <select style={controlStyle} value={epicFilter} onChange={(e) => setEpicFilter(e.target.value)}>
              <option value="">All epics</option>
              {epics.map((epic) => (
                <option key={epic.id} value={epic.id}>
                  {epic.title}
                </option>
              ))}
            </select>
            <input
              style={controlStyle}
              placeholder="Search title…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <Button onClick={() => setOverlay({ kind: 'create' })}>New ticket</Button>
          </>
        )}
      </div>

      {error && <p style={{ color: '#b00020' }}>{error}</p>}
      {!teamId && <p>Select a team to open its board.</p>}

      {teamId && (
        <DndContext sensors={sensors} onDragEnd={onDragEnd} autoScroll={false}>
          <div
            style={{
              display: 'flex',
              gap: '0.75rem',
              overflowX: 'auto',
              paddingBottom: '0.5rem',
              alignItems: 'stretch',
              minHeight: 'calc(100vh - 190px)',
            }}
          >
            {TICKET_STATES.map((state) => (
              <Column key={state} state={state} count={columns.get(state)!.length}>
                {columns.get(state)!.map((ticket) => (
                  <TicketCard
                    key={ticket.id}
                    ticket={ticket}
                    epicTitle={epicTitle(ticket.epicId)}
                    onOpen={() => setOverlay({ kind: 'details', ticket })}
                  />
                ))}
              </Column>
            ))}
          </div>
        </DndContext>
      )}

      {overlay.kind !== 'none' && (
        <Modal onClose={closeOverlay}>
          {overlay.kind === 'create' && (
            <TicketForm teams={teams} ticket={null} defaultTeamId={teamId} onSaved={closeOverlay} onCancel={closeOverlay} />
          )}
          {overlay.kind === 'edit' && (
            <TicketForm teams={teams} ticket={overlay.ticket} defaultTeamId={teamId} onSaved={closeOverlay} onCancel={closeOverlay} />
          )}
          {overlay.kind === 'details' && (
            <TicketDetails
              ticket={overlay.ticket}
              epicTitle={epicTitle(overlay.ticket.epicId)}
              onEdit={() => setOverlay({ kind: 'edit', ticket: overlay.ticket })}
              onDelete={() => deleteTicket(overlay.ticket.id)}
              onBack={closeOverlay}
            />
          )}
        </Modal>
      )}
    </div>
  );
}

function Column({ state, count, children }: { state: TicketState; count: number; children: ReactNode }) {
  const { setNodeRef, isOver } = useDroppable({ id: state });
  const palette = stateColors[state];
  return (
    <div
      ref={setNodeRef}
      style={{
        flex: '1 0 220px',
        minWidth: 220,
        background: palette.bg,
        borderRadius: 8,
        padding: '0.5rem',
        borderTop: `3px solid ${palette.accent}`,
        outline: isOver ? `2px solid ${palette.accent}` : '2px solid transparent',
        transition: 'outline-color 0.1s',
      }}
    >
      <h3 style={{ fontSize: '0.85rem', margin: '0 0 0.5rem', color: palette.accent }}>
        {stateLabel(state)} <span style={{ opacity: 0.7 }}>({count})</span>
      </h3>
      <div style={{ display: 'grid', gap: '0.5rem', minHeight: 40 }}>{children}</div>
    </div>
  );
}

function TicketCard({ ticket, epicTitle, onOpen }: { ticket: Ticket; epicTitle: string; onOpen: () => void }) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({ id: ticket.id });
  const style: CSSProperties = {
    background: '#fff',
    border: '1px solid #ddd',
    borderRadius: 6,
    padding: '0.5rem',
    boxShadow: '0 1px 2px rgba(0,0,0,0.08)',
    cursor: 'grab',
    opacity: isDragging ? 0.5 : 1,
    transform: transform ? `translate3d(${transform.x}px, ${transform.y}px, 0)` : undefined,
  };
  const typePalette = typeColors[ticket.type];
  return (
    <div ref={setNodeRef} style={style} {...listeners} {...attributes} onClick={onOpen}>
      <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{ticket.title}</div>
      <div style={{ display: 'flex', gap: '0.4rem', marginTop: '0.35rem', flexWrap: 'wrap' }}>
        <span style={{ ...badge, background: typePalette.bg, color: typePalette.fg }}>{ticket.type}</span>
        {ticket.epicId && <span style={{ ...badge, background: '#eee', color: '#555' }}>{epicTitle}</span>}
      </div>
    </div>
  );
}

function Modal({ children, onClose }: { children: ReactNode; onClose: () => void }) {
  return (
    <div style={modalBackdrop} onClick={onClose}>
      <div style={modalPanel} onClick={(e) => e.stopPropagation()}>
        {children}
      </div>
    </div>
  );
}

const controlStyle: CSSProperties = {
  padding: '0.4rem 0.5rem',
  border: '1px solid #ccc',
  borderRadius: 4,
};

const badge: CSSProperties = {
  fontSize: '0.7rem',
  padding: '0.1rem 0.5rem',
  borderRadius: 999,
  background: '#eef',
  color: '#334',
};

const modalBackdrop: CSSProperties = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(0,0,0,0.4)',
  display: 'grid',
  placeItems: 'center',
  padding: '1rem',
  zIndex: 10,
};

const modalPanel: CSSProperties = {
  background: '#fff',
  borderRadius: 8,
  padding: '1.5rem',
  width: 560,
  maxWidth: '100%',
  maxHeight: '90vh',
  overflowY: 'auto',
};
