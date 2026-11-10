import './StatusBadge.css';

const BADGE_CONFIG = {
  free:     { bg: '#C0DD97', dot: '#5D8623', text: 'FREE' },
  ghost:    { bg: '#FAC775', dot: '#DF8E0B', text: 'FREE' },
  occupied: { bg: '#F09595', dot: '#B02626', text: 'OCCUPIED' },
  squatter: { bg: '#BEBEBE', dot: '#6B6761', text: 'OCCUPIED' },
};

export default function StatusBadge({ status }) {
  const { bg, dot, text } = BADGE_CONFIG[status];
  return (
    <span className="status-badge" style={{ background: bg }}>
      <span className="status-badge__dot" style={{ background: dot }} />
      <span className="status-badge__text">{text}</span>
    </span>
  );
}
