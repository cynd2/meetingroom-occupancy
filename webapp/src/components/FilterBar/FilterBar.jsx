import './FilterBar.css';

const FILTERS = ['All', 'Free', 'Occupied'];

export default function FilterBar({ active, onFilter, clock }) {
  return (
    <div className="filterbar">
      <div className="filterbar__buttons">
        {FILTERS.map((f) => (
          <button
            key={f}
            className={`filterbar__btn${active === f ? ' filterbar__btn--active' : ''}`}
            onClick={() => onFilter(f)}
          >
            {f}
          </button>
        ))}
      </div>
      <span className="filterbar__clock">{clock}</span>
    </div>
  );
}
