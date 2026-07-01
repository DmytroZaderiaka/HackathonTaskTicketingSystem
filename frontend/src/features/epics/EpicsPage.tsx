import { type CSSProperties, type FormEvent, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import type { ApiError } from '../../api/client';
import { type Epic, epicsApi } from '../../api/epics';
import { type Team, teamsApi } from '../../api/teams';

export function EpicsPage() {
  const [teams, setTeams] = useState<Team[] | null>(null);
  const [teamId, setTeamId] = useState('');
  const [epics, setEpics] = useState<Epic[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [creating, setCreating] = useState(false);
  const submitting = useRef(false);

  useEffect(() => {
    teamsApi
      .list()
      .then(setTeams)
      .catch((err: ApiError) => setError(err.title));
  }, []);

  const loadEpics = (id: string) => {
    if (!id) {
      setEpics(null);
      return;
    }
    epicsApi
      .list(id)
      .then(setEpics)
      .catch((err: ApiError) => setError(err.title));
  };

  useEffect(() => loadEpics(teamId), [teamId]);

  const create = async (event: FormEvent) => {
    event.preventDefault();
    if (!teamId || submitting.current) {
      return;
    }
    submitting.current = true;
    setError(null);
    setCreating(true);
    try {
      await epicsApi.create(teamId, { title, description: description.trim() ? description : null });
      setTitle('');
      setDescription('');
      loadEpics(teamId);
    } catch (err) {
      setError((err as ApiError).title);
    } finally {
      setCreating(false);
      submitting.current = false;
    }
  };

  return (
    <main style={pageStyle}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>Epics</h1>
        <Link to="/">← Back</Link>
      </header>

      <label style={{ display: 'block', margin: '1rem 0 0.5rem' }}>
        Team
        <select style={{ ...inputStyle, display: 'block', width: '100%' }} value={teamId} onChange={(e) => setTeamId(e.target.value)}>
          <option value="">— Select a team —</option>
          {(teams ?? []).map((team) => (
            <option key={team.id} value={team.id}>
              {team.name}
            </option>
          ))}
        </select>
      </label>

      {error && <p style={errorText}>{error}</p>}

      {!teamId && <p>Select a team to manage its epics.</p>}

      {teamId && (
        <>
          <form onSubmit={create} style={{ display: 'grid', gap: '0.5rem', margin: '1rem 0' }}>
            <input
              style={inputStyle}
              placeholder="Epic title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
            />
            <textarea
              style={{ ...inputStyle, minHeight: 60, resize: 'vertical' }}
              placeholder="Description (optional)"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
            <button style={primaryButton} type="submit" disabled={creating}>
              {creating ? 'Adding…' : 'Add epic'}
            </button>
          </form>

          {epics === null && <p>Loading…</p>}
          {epics !== null && epics.length === 0 && <p>No epics in this team yet.</p>}
          {epics !== null && epics.length > 0 && (
            <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {epics.map((epic) => (
                <EpicRow key={epic.id} epic={epic} onChanged={() => loadEpics(teamId)} onError={setError} />
              ))}
            </ul>
          )}
        </>
      )}
    </main>
  );
}

function EpicRow({
  epic,
  onChanged,
  onError,
}: {
  epic: Epic;
  onChanged: () => void;
  onError: (message: string) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [title, setTitle] = useState(epic.title);
  const [description, setDescription] = useState(epic.description ?? '');
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [busy, setBusy] = useState(false);
  const inFlight = useRef(false);

  const run = async (action: () => Promise<unknown>, after: () => void) => {
    if (inFlight.current) {
      return;
    }
    inFlight.current = true;
    setBusy(true);
    try {
      await action();
      after();
      onChanged();
    } catch (err) {
      onError((err as ApiError).title);
    } finally {
      setBusy(false);
      inFlight.current = false;
    }
  };

  const save = () =>
    run(
      () => epicsApi.update(epic.id, { title, description: description.trim() ? description : null }),
      () => setEditing(false),
    );

  const remove = () => run(() => epicsApi.remove(epic.id), () => setConfirmingDelete(false));

  return (
    <li style={rowStyle}>
      {editing ? (
        <div style={{ display: 'grid', gap: '0.5rem', width: '100%' }}>
          <input style={inputStyle} value={title} onChange={(e) => setTitle(e.target.value)} />
          <textarea
            style={{ ...inputStyle, minHeight: 50, resize: 'vertical' }}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button style={secondaryButton} onClick={save} disabled={busy}>
              Save
            </button>
            <button
              style={secondaryButton}
              onClick={() => {
                setTitle(epic.title);
                setDescription(epic.description ?? '');
                setEditing(false);
              }}
              disabled={busy}
            >
              Cancel
            </button>
          </div>
        </div>
      ) : confirmingDelete ? (
        <>
          <span style={{ flex: 1 }}>Delete “{epic.title}”?</span>
          <button style={dangerButton} onClick={remove} disabled={busy}>
            Delete
          </button>
          <button style={secondaryButton} onClick={() => setConfirmingDelete(false)} disabled={busy}>
            Cancel
          </button>
        </>
      ) : (
        <>
          <div style={{ flex: 1 }}>
            <strong>{epic.title}</strong>
            {epic.description && <div style={{ color: '#555', fontSize: '0.9rem' }}>{epic.description}</div>}
          </div>
          <button style={secondaryButton} onClick={() => setEditing(true)}>
            Edit
          </button>
          <button style={secondaryButton} onClick={() => setConfirmingDelete(true)}>
            Delete
          </button>
        </>
      )}
    </li>
  );
}

const pageStyle: CSSProperties = {
  fontFamily: 'system-ui, sans-serif',
  padding: '2rem',
  maxWidth: 640,
  margin: '0 auto',
};

const inputStyle: CSSProperties = {
  padding: '0.5rem',
  border: '1px solid #ccc',
  borderRadius: 4,
  boxSizing: 'border-box',
};

const rowStyle: CSSProperties = {
  display: 'flex',
  gap: '0.5rem',
  alignItems: 'flex-start',
  padding: '0.5rem 0',
  borderBottom: '1px solid #eee',
};

const primaryButton: CSSProperties = {
  padding: '0.5rem 1rem',
  border: 'none',
  borderRadius: 4,
  background: '#0052cc',
  color: '#fff',
  cursor: 'pointer',
  justifySelf: 'start',
};

const secondaryButton: CSSProperties = {
  padding: '0.4rem 0.75rem',
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

const errorText: CSSProperties = {
  color: '#b00020',
};
