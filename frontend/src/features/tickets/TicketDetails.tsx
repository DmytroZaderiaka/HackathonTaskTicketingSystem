import { type CSSProperties, useRef, useState } from 'react';
import { type Ticket, stateLabel } from '../../api/tickets';
import { Button } from '../../components/Button';
import { stateColors, typeColors } from '../../components/theme';
import { TicketComments } from './TicketComments';

interface TicketDetailsProps {
  ticket: Ticket;
  epicTitle: string;
  onEdit: () => void;
  onDelete: () => void;
  onBack: () => void;
}

export function TicketDetails({ ticket, epicTitle, onEdit, onDelete, onBack }: TicketDetailsProps) {
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
        ← Back
      </button>
      <h2>{ticket.title}</h2>
      <dl style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '0.25rem 1rem' }}>
        <dt style={dt}>Type</dt>
        <dd style={dd}>
          <span style={{ ...badge, background: typeColors[ticket.type].bg, color: typeColors[ticket.type].fg }}>
            {ticket.type}
          </span>
        </dd>
        <dt style={dt}>State</dt>
        <dd style={dd}>
          <span style={{ ...badge, background: stateColors[ticket.state].bg, color: stateColors[ticket.state].accent }}>
            {stateLabel(ticket.state)}
          </span>
        </dd>
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
        <Button onClick={onEdit}>Edit</Button>
        {confirming ? (
          <>
            <Button variant="danger" onClick={confirmDelete}>
              Confirm delete
            </Button>
            <Button variant="secondary" onClick={() => setConfirming(false)}>
              Cancel
            </Button>
          </>
        ) : (
          <Button variant="secondary" onClick={() => setConfirming(true)}>
            Delete
          </Button>
        )}
      </div>

      <TicketComments ticketId={ticket.id} />
    </>
  );
}

const badge: CSSProperties = {
  fontSize: '0.75rem',
  padding: '0.1rem 0.5rem',
  borderRadius: 999,
  fontWeight: 600,
};

const linkButton: CSSProperties = {
  border: 'none',
  background: 'none',
  color: '#0052cc',
  cursor: 'pointer',
  padding: 0,
  font: 'inherit',
};

const dt: CSSProperties = { fontWeight: 600, color: '#555' };
const dd: CSSProperties = { margin: 0 };
