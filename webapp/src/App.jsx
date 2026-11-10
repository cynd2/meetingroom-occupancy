import { useState, useEffect, useCallback } from 'react';
import TopBar from './components/TopBar/TopBar';
import FilterBar from './components/FilterBar/FilterBar';
import FloorSection from './components/FloorSection/FloorSection';
import './App.css';

const POLL_INTERVAL = 60_000;
const STATUS_ORDER  = { free: 0, ghost: 1, occupied: 2, squatter: 3 };

function wibClock() {
  const now = new Date();
  const wib = new Date(now.getTime() + 7 * 60 * 60 * 1000);
  const hh = String(wib.getUTCHours()).padStart(2, '0');
  const mm = String(wib.getUTCMinutes()).padStart(2, '0');
  const ss = String(wib.getUTCSeconds()).padStart(2, '0');
  return `${hh}:${mm}:${ss} WIB`;
}

export default function App() {
  const [filter, setFilter]   = useState('All');
  const [clock, setClock]     = useState(wibClock);
  const [floors, setFloors]   = useState([]);
  const [summary, setSummary] = useState({ total: 0, free: 0, occupied: 0 });
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState(null);

  // Live clock, ticks every second
  useEffect(() => {
    const id = setInterval(() => setClock(wibClock()), 1000);
    return () => clearInterval(id);
  }, []);

  const fetchRooms = useCallback(async () => {
    try {
      const res = await fetch('/api/rooms');
      if (!res.ok) throw new Error(`Server error ${res.status}`);
      const data = await res.json();
      setFloors(data.floors ?? []);
      setSummary(data.summary ?? { total: 0, free: 0, occupied: 0 });
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  // Fetch on mount, then every 60 s
  useEffect(() => {
    fetchRooms();
    const id = setInterval(fetchRooms, POLL_INTERVAL);
    return () => clearInterval(id);
  }, [fetchRooms]);

  return (
    <>
      <TopBar summary={summary} />
      <FilterBar active={filter} onFilter={setFilter} clock={clock} />

      {loading && (
        <p style={{ padding: '40px 80px', fontFamily: 'JetBrains Mono, monospace' }}>
          Loading rooms…
        </p>
      )}

      {!loading && error && (
        <p style={{ padding: '40px 80px', color: '#B02626', fontFamily: 'JetBrains Mono, monospace' }}>
          Could not reach the server: {error}
        </p>
      )}

      {!loading && !error && (
        <FloorSection
          floor={{
            label: '',
            rooms: floors
              .flatMap((f) => f.rooms)
              .sort((a, b) => STATUS_ORDER[a.status] - STATUS_ORDER[b.status]),
          }}
          filter={filter}
        />
      )}
    </>
  );
}
