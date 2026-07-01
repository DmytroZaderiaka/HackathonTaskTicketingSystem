import { type CSSProperties, useRef, useState } from 'react';
import { type Ticket, stateLabel } from '../../api/tickets';
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
};

const dt: CSSProperties = { fontWeight: 600, color: '#555' };
const dd: CSSProperties = { margin: 0 };
