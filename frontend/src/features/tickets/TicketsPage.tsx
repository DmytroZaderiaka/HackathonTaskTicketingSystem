import { type CSSProperties, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import type { ApiError } from '../../api/client';
import { type Epic, epicsApi } from '../../api/epics';
import { type Team, teamsApi } from '../../api/teams';
import { type Ticket, stateLabel, ticketsApi } from '../../api/tickets';
import { TicketComments } from './TicketComments';
import { TicketForm } from './TicketForm';

type View = 'list' | 'create' | 'edit' | 'details';

export function TicketsPage() {
  const [teams, setTeams] = useState<Team[]>([]);
  const [teamId, setTeamId] = useState('');
  const [tickets, setTickets] = useState<Ticket[] | null>(null);
  const [epicsById, setEpicsById] = useState<Map<string, Epic>>(new Map());
  const [error, setError] = useState<string | null>(null);
  const [view, setView] = useState<View>('list');
  const [selected, setSelected] = useState<Ticket | null>(null);

  useEffect(() => {
    teamsApi
      .list()
      .then(setTeams)
      .catch((err: ApiError) => setError(err.title));
  }, []);

  const loadTickets = (id: string) => {
    if (!id) {
      setTickets(null);
      return;
    }
    setError(null);
    ticketsApi
      .list(id)
      .then(setTickets)
      .catch((err: ApiError) => setError(err.title));
    epicsApi
      .list(id)
      .then((list) => setEpicsById(new Map(list.map((e) => [e.id, e]))))
      .catch(() => setEpicsById(new Map()));
  };

  useEffect(() => loadTickets(teamId), [teamId]);

  const backToList = () => {
    setSelected(null);
    setView('list');
    loadTickets(teamId);
  };

  const openDetails = async (id: string) => {
    try {
      const ticket = await ticketsApi.get(id);
      setSelected(ticket);
      setView('details');
    } catch (err) {
      setError((err as ApiError).title);
    }
  };

  const remove = async (id: string) => {
    try {
      await ticketsApi.remove(id);
      backToList();
    } catch (err) {
      setError((err as ApiError).title);
    }
  };

  const epicTitle = (epicId: string | null) => (epicId ? (epicsById.get(epicId)?.title ?? '—') : '—');

  if (view === 'create' || (view === 'edit' && selected)) {
    return (
      <main style={pageStyle}>
        <TicketForm
          teams={teams}
          ticket={view === 'edit' ? selected : null}
          defaultTeamId={teamId}
          onSaved={backToList}
          onCancel={backToList}
        />
      </main>
    );
  }

  if (view === 'details' && selected) {
    return (
      <main style={pageStyle}>
        <TicketDetails
          ticket={selected}
          epicTitle={epicTitle(selected.epicId)}
          onEdit={() => setView('edit')}
          onDelete={() => remove(selected.id)}
          onBack={backToList}
        />
      </main>
    );
  }

  return (
    <main style={pageStyle}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>Tickets</h1>
        <Link to="/">← Back</Link>
      </header>

      <label style={{ display: 'block', margin: '1rem 0 0.5rem' }}>
        Team
        <select style={selectStyle} value={teamId} onChange={(e) => setTeamId(e.target.value)}>
          <option value="">— Select a team —</option>
          {teams.map((team) => (
            <option key={team.id} value={team.id}>
              {team.name}
            </option>
          ))}
        </select>
      </label>

      {error && <p style={{ color: '#b00020' }}>{error}</p>}
      {!teamId && <p>Select a team to see its tickets.</p>}

      {teamId && (
        <>
          <button style={primaryButton} onClick={() => setView('create')}>
            New ticket
          </button>

          {tickets === null && <p>Loading…</p>}
          {tickets !== null && tickets.length === 0 && <p>No tickets in this team yet.</p>}
          {tickets !== null && tickets.length > 0 && (
            <ul style={{ listStyle: 'none', padding: 0, margin: '1rem 0 0' }}>
              {tickets.map((ticket) => (
                <li key={ticket.id} style={rowStyle}>
                  <button style={linkButton} onClick={() => openDetails(ticket.id)}>
                    {ticket.title}
                  </button>
                  <span style={badge}>{ticket.type}</span>
                  <span style={badge}>{stateLabel(ticket.state)}</span>
                  <span style={{ color: '#777', fontSize: '0.85rem' }}>{epicTitle(ticket.epicId)}</span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </main>
  );
}

function TicketDetails({
  ticket,
  epicTitle,
  onEdit,
  onDelete,
  onBack,
}: {
  ticket: Ticket;
  epicTitle: string;
  onEdit: () => void;
  onDelete: () => void;
  onBack: () => void;
}) {
  const [confirming, setConfirming] = useState(false);
  const deleting = useRef(false);

  const confirmDelete = () => {
    if (deleting.current) {
      return;
    }
    deleting.current = true;
    onDelete();
  };

  return (
    <>
      <button style={linkButton} onClick={onBack}>
        ← Back to list
      </button>
      <h2>{ticket.title}</h2>
      <dl style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '0.25rem 1rem' }}>
        <dt style={dt}>Type</dt>
        <dd style={dd}>{ticket.type}</dd>
        <dt style={dt}>State</dt>
        <dd style={dd}>{stateLabel(ticket.state)}</dd>
        <dt style={dt}>Epic</dt>
        <dd style={dd}>{epicTitle}</dd>
        <dt style={dt}>Created by</dt>
        <dd style={dd}>{ticket.createdBy.email}</dd>
        <dt style={dt}>Created</dt>
        <dd style={dd}>{new Date(ticket.createdAt).toLocaleString()}</dd>
        <dt style={dt}>Modified</dt>
        <dd style={dd}>{new Date(ticket.modifiedAt).toLocaleString()}</dd>
      </dl>
      <h3>Body</h3>
      <p style={{ whiteSpace: 'pre-wrap' }}>{ticket.body}</p>

      <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
        <button style={primaryButton} onClick={onEdit}>
          Edit
        </button>
        {confirming ? (
          <>
            <button style={dangerButton} onClick={confirmDelete}>
              Confirm delete
            </button>
            <button style={secondaryButton} onClick={() => setConfirming(false)}>
              Cancel
            </button>
          </>
        ) : (
          <button style={secondaryButton} onClick={() => setConfirming(true)}>
            Delete
          </button>
        )}
      </div>

      <TicketComments ticketId={ticket.id} />
    </>
  );
}

const pageStyle: CSSProperties = {
  fontFamily: 'system-ui, sans-serif',
  padding: '2rem',
  maxWidth: 720,
  margin: '0 auto',
};

const selectStyle: CSSProperties = {
  display: 'block',
  width: '100%',
  padding: '0.5rem',
  border: '1px solid #ccc',
  borderRadius: 4,
};

const rowStyle: CSSProperties = {
  display: 'flex',
  gap: '0.75rem',
  alignItems: 'center',
  padding: '0.5rem 0',
  borderBottom: '1px solid #eee',
};

const badge: CSSProperties = {
  fontSize: '0.75rem',
  padding: '0.1rem 0.5rem',
  borderRadius: 999,
  background: '#eef',
  color: '#334',
};

const primaryButton: CSSProperties = {
  padding: '0.5rem 1rem',
  border: 'none',
  borderRadius: 4,
  background: '#0052cc',
  color: '#fff',
  cursor: 'pointer',
};

const secondaryButton: CSSProperties = {
  padding: '0.5rem 1rem',
  border: '1px solid #ccc',
  borderRadius: 4,
  background: '#fff',
  cursor: 'pointer',
};

const dangerButton: CSSProperties = {
  ...secondaryButton,
  border: '1px solid #b00020',
  color: '#b00020',
};

const linkButton: CSSProperties = {
  border: 'none',
  background: 'none',
  color: '#0052cc',
  cursor: 'pointer',
  padding: 0,
  font: 'inherit',
  textAlign: 'left',
  flex: 1,
};

const dt: CSSProperties = { fontWeight: 600, color: '#555' };
const dd: CSSProperties = { margin: 0 };
