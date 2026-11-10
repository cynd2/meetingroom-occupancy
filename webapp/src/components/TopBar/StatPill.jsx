import './StatPill.css';

export default function StatPill({ count, label }) {
  return (
    <div className="stat-pill">
      <span className="stat-pill__count">{count}</span>
      <span className="stat-pill__label">{label}</span>
    </div>
  );
}
