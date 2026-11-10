import StatPill from './StatPill';
import './TopBar.css';

export default function TopBar({ summary }) {
  return (
    <div className="topbar">
      <div className="topbar__left">
        <h1 className="topbar__title">Meeting room status</h1>
        <p className="topbar__live">
          <span className="topbar__live-dot" />
          Live · refreshes every 60 s · Jakarta time (WIB)
        </p>
      </div>
      <div className="topbar__pills">
        <StatPill count={summary.total} label="All Rooms" />
        <StatPill count={summary.free} label="Free" />
        <StatPill count={summary.occupied} label="Occupied" />
      </div>
    </div>
  );
}
