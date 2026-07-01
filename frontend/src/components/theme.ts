import type { TicketState, TicketType } from '../api/tickets';

/** Accent + tint per board column / ticket state. */
export const stateColors: Record<TicketState, { bg: string; accent: string }> = {
  new: { bg: '#eef1f5', accent: '#64748b' }, // slate
  ready_for_implementation: { bg: '#e7f0ff', accent: '#2563eb' }, // blue
  in_progress: { bg: '#fff4e5', accent: '#d97706' }, // amber
  ready_for_acceptance: { bg: '#f3e8ff', accent: '#7c3aed' }, // purple
  done: { bg: '#e6f6ec', accent: '#16a34a' }, // green
};

/** Badge colors per ticket type. */
export const typeColors: Record<TicketType, { bg: string; fg: string }> = {
  bug: { bg: '#fde8e8', fg: '#c81e1e' }, // red
  feature: { bg: '#e7f0ff', fg: '#1d4ed8' }, // blue
  fix: { bg: '#fff4e5', fg: '#b45309' }, // amber
};

export const colors = {
  primary: '#0052cc',
  text: '#172b4d',
  muted: '#6b7280',
  border: '#dfe1e6',
  danger: '#c81e1e',
  pageBg: '#f4f5f7',
};
