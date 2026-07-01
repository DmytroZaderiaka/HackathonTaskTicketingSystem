import { type CSSProperties, type FormEvent, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import type { ApiError } from '../../api/client';
import { type Team, teamsApi } from '../../api/teams';

export function TeamsPage() {
  const [teams, setTeams] = useState<Team[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);
  const submitting = useRef(false);

  const load = () => {
    teamsApi
      .list()
      .then(setTeams)
      .catch((err: ApiError) => setError(err.title));
  };

  useEffect(load, []);

  const create = async (event: FormEvent) => {
    event.preventDefault();

    // Synchronous guard: the `disabled` attribute is not applied until the next
    // render, so a fast double-click could otherwise submit twice.
    if (submitting.current) {
      return;
    }
    submitting.current = true;

    setError(null);
    setCreating(true);
    try {
      await teamsApi.create(newName);
      setNewName('');
      load();
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
        <h1>Teams</h1>
        <Link to="/">← Back</Link>
      </header>

      <form onSubmit={create} style={{ display: 'flex', gap: '0.5rem', margin: '1rem 0' }}>
        <input
          style={{ ...inputStyle, flex: 1 }}
          placeholder="New team name"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          required
        />
        <button style={primaryButton} type="submit" disabled={creating}>
          {creating ? 'Adding…' : 'Add team'}
        </button>
      </form>

      {error && <p style={errorText}>{error}</p>}

      {teams === null && <p>Loading…</p>}
      {teams !== null && teams.length === 0 && <p>No teams yet. Create the first one above.</p>}

      {teams !== null && teams.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
          {teams.map((team) => (
            <TeamRow key={team.id} team={team} onChanged={load} onError={setError} />
          ))}
        </ul>
      )}
    </main>
  );
}

function TeamRow({
  team,
  onChanged,
  onError,
}: {
  team: Team;
  onChanged: () => void;
  onError: (message: string) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(team.name);
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

  const save = () => run(() => teamsApi.rename(team.id, name), () => setEditing(false));

  const remove = () => run(() => teamsApi.remove(team.id), () => setConfirmingDelete(false));

  return (
    <li style={rowStyle}>
      {editing ? (
        <>
          <input style={{ ...inputStyle, flex: 1 }} value={name} onChange={(e) => setName(e.target.value)} />
          <button style={secondaryButton} onClick={save} disabled={busy}>
            Save
          </button>
          <button
            style={secondaryButton}
            onClick={() => {
              setName(team.name);
              setEditing(false);
            }}
            disabled={busy}
          >
            Cancel
          </button>
        </>
      ) : confirmingDelete ? (
        <>
          <span style={{ flex: 1 }}>Delete “{team.name}”?</span>
          <button style={dangerButton} onClick={remove} disabled={busy}>
            Delete
          </button>
          <button style={secondaryButton} onClick={() => setConfirmingDelete(false)} disabled={busy}>
            Cancel
          </button>
        </>
      ) : (
        <>
          <span style={{ flex: 1 }}>{team.name}</span>
          <button style={secondaryButton} onClick={() => setEditing(true)}>
            Rename
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
};

const rowStyle: CSSProperties = {
  display: 'flex',
  gap: '0.5rem',
  alignItems: 'center',
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
