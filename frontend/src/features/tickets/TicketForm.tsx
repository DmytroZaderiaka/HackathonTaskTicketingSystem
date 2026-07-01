import { type CSSProperties, type FormEvent, useEffect, useRef, useState } from 'react';
import type { ApiError } from '../../api/client';
import { type Epic, epicsApi } from '../../api/epics';
import type { Team } from '../../api/teams';
import {
  TICKET_STATES,
  TICKET_TYPES,
  type Ticket,
  type TicketInput,
  type TicketState,
  type TicketType,
  stateLabel,
  ticketsApi,
} from '../../api/tickets';
import { Button } from '../../components/Button';

interface TicketFormProps {
  teams: Team[];
  ticket: Ticket | null; // null => create
  defaultTeamId: string;
  onSaved: () => void;
  onCancel: () => void;
}

export function TicketForm({ teams, ticket, defaultTeamId, onSaved, onCancel }: TicketFormProps) {
  const [teamId, setTeamId] = useState(ticket?.teamId ?? defaultTeamId);
  const [epicId, setEpicId] = useState(ticket?.epicId ?? '');
  const [type, setType] = useState<TicketType>(ticket?.type ?? 'bug');
  const [state, setState] = useState<TicketState>(ticket?.state ?? 'new');
  const [title, setTitle] = useState(ticket?.title ?? '');
  const [body, setBody] = useState(ticket?.body ?? '');
  const [epics, setEpics] = useState<Epic[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const submitting = useRef(false);

  useEffect(() => {
    if (!teamId) {
      setEpics([]);
      return;
    }
    epicsApi
      .list(teamId)
      .then(setEpics)
      .catch((err: ApiError) => setError(err.title));
  }, [teamId]);

  // Changing the team clears the selected epic (an epic belongs to a single team).
  const onTeamChange = (nextTeamId: string) => {
    setTeamId(nextTeamId);
    setEpicId('');
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (submitting.current) {
      return;
    }
    submitting.current = true;
    setError(null);
    setBusy(true);
    const input: TicketInput = { teamId, epicId: epicId || null, type, state, title, body };
    try {
      if (ticket) {
        await ticketsApi.update(ticket.id, input);
      } else {
        await ticketsApi.create(input);
      }
      onSaved();
    } catch (err) {
      setError((err as ApiError).title);
    } finally {
      setBusy(false);
      submitting.current = false;
    }
  };

  return (
    <form onSubmit={submit} style={{ display: 'grid', gap: '0.75rem' }}>
      <h2>{ticket ? 'Edit ticket' : 'New ticket'}</h2>

      <label style={labelStyle}>
        Team
        <select style={inputStyle} value={teamId} onChange={(e) => onTeamChange(e.target.value)} required>
          <option value="">— Select a team —</option>
          {teams.map((team) => (
            <option key={team.id} value={team.id}>
              {team.name}
            </option>
          ))}
        </select>
      </label>

      <label style={labelStyle}>
        Epic
        <select style={inputStyle} value={epicId} onChange={(e) => setEpicId(e.target.value)}>
          <option value="">— None —</option>
          {epics.map((epic) => (
            <option key={epic.id} value={epic.id}>
              {epic.title}
            </option>
          ))}
        </select>
      </label>

      <div style={{ display: 'flex', gap: '0.75rem' }}>
        <label style={{ ...labelStyle, flex: 1 }}>
          Type
          <select style={inputStyle} value={type} onChange={(e) => setType(e.target.value as TicketType)}>
            {TICKET_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </label>
        <label style={{ ...labelStyle, flex: 1 }}>
          State
          <select style={inputStyle} value={state} onChange={(e) => setState(e.target.value as TicketState)}>
            {TICKET_STATES.map((s) => (
              <option key={s} value={s}>
                {stateLabel(s)}
              </option>
            ))}
          </select>
        </label>
      </div>

      <label style={labelStyle}>
        Title
        <input style={inputStyle} value={title} onChange={(e) => setTitle(e.target.value)} required />
      </label>

      <label style={labelStyle}>
        Body
        <textarea
          style={{ ...inputStyle, minHeight: 120, resize: 'vertical' }}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          required
        />
      </label>

      {error && <p style={{ color: '#b00020', margin: 0 }}>{error}</p>}

      <div style={{ display: 'flex', gap: '0.5rem' }}>
        <Button type="submit" disabled={busy}>
          {busy ? 'Saving…' : 'Save'}
        </Button>
        <Button variant="secondary" type="button" onClick={onCancel} disabled={busy}>
          Cancel
        </Button>
      </div>
    </form>
  );
}

const labelStyle: CSSProperties = {
  display: 'grid',
  gap: '0.25rem',
  fontSize: '0.9rem',
  color: '#333',
};

const inputStyle: CSSProperties = {
  padding: '0.5rem',
  border: '1px solid #ccc',
  borderRadius: 4,
  boxSizing: 'border-box',
  width: '100%',
};
