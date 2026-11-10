import RoomCard from '../RoomCard/RoomCard';
import './FloorSection.css';

export default function FloorSection({ floor, filter }) {
  const filtered = floor.rooms.filter((room) => {
    if (filter === 'All') return true;
    if (filter === 'Free') return room.status === 'free' || room.status === 'ghost';
    if (filter === 'Occupied') return room.status === 'occupied' || room.status === 'squatter';
    return true;
  });

  if (filtered.length === 0) return null;

  return (
    <section className="floor-section">
      {floor.label && (
        <div className="floor-section__header">
          <span className="floor-section__label">{floor.label}</span>
          <div className="floor-section__line" />
        </div>
      )}
      <div className="floor-section__grid">
        {filtered.map((room) => (
          <RoomCard key={room.id} room={room} />
        ))}
      </div>
    </section>
  );
}
