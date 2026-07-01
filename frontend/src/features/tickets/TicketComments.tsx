import { type CSSProperties, type FormEvent, useEffect, useRef, useState } from 'react';
import type { ApiError } from '../../api/client';
import { type Comment, commentsApi } from '../../api/comments';
import { Button } from '../../components/Button';

export function TicketComments({ ticketId }: { ticketId: string }) {
  const [comments, setComments] = useState<Comment[] | null>(null);
  const [body, setBody] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const submitting = useRef(false);

  const load = () => {
    commentsApi
      .list(ticketId)
      .then(setComments)
      .catch((err: ApiError) => setError(err.title));
  };

  useEffect(load, [ticketId]);

  const add = async (event: FormEvent) => {
    event.preventDefault();
    if (submitting.current) {
      return;
    }
    submitting.current = true;
    setError(null);
    setBusy(true);
    try {
      await commentsApi.add(ticketId, body);
      setBody('');
      load();
    } catch (err) {
      setError((err as ApiError).title);
    } finally {
      setBusy(false);
      submitting.current = false;
    }
  };

  return (
    <section style={{ marginTop: '1.5rem' }}>
      <h3>Comments</h3>

      {comments === null && <p>Loading…</p>}
      {comments !== null && comments.length === 0 && <p style={{ color: '#777' }}>No comments yet.</p>}
      {comments !== null && comments.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
          {comments.map((comment) => (
            <li key={comment.id} style={commentStyle}>
              <div style={{ fontSize: '0.8rem', color: '#666' }}>
                {comment.author.email} · {new Date(comment.createdAt).toLocaleString()}
              </div>
              <div style={{ whiteSpace: 'pre-wrap' }}>{comment.body}</div>
            </li>
          ))}
        </ul>
      )}

      <form onSubmit={add} style={{ display: 'grid', gap: '0.5rem', marginTop: '0.75rem' }}>
        <textarea
          style={textareaStyle}
          placeholder="Add a comment…"
          value={body}
          onChange={(e) => setBody(e.target.value)}
          required
        />
        {error && <p style={{ color: '#b00020', margin: 0 }}>{error}</p>}
        <Button type="submit" disabled={busy} style={{ justifySelf: 'start' }}>
          {busy ? 'Posting…' : 'Add comment'}
        </Button>
      </form>
    </section>
  );
}

const commentStyle: CSSProperties = {
  padding: '0.5rem 0',
  borderBottom: '1px solid #eee',
};

const textareaStyle: CSSProperties = {
  padding: '0.5rem',
  border: '1px solid #ccc',
  borderRadius: 4,
  minHeight: 60,
  resize: 'vertical',
  boxSizing: 'border-box',
};

