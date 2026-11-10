import StatusBadge from './StatusBadge';
import ActivityChart from './ActivityChart';
import './RoomCard.css';

function CardBody({ room }) {
  const { status, nextBooking, lastChecked, startsAt, endsAt, endsAtMinsLeft, emptyForMins, occupiedForMins } = room;

  if (status === 'free') {
    return (
      <div className="card__body">
        <div className="card__meta-row">
          <span>next booking</span>
          <span>{nextBooking?.range ?? '—'}</span>
        </div>
        {nextBooking && (
          <div className="card__meta-row">
            <span>begin at</span>
            <span>{nextBooking.beginAt} ({nextBooking.minsLeft} min left)</span>
          </div>
        )}
        {lastChecked && (
          <div className="card__meta-row">
            <span>last checked</span>
            <span>{lastChecked}</span>
          </div>
        )}
      </div>
    );
  }

  if (status === 'occupied') {
    return (
      <div className="card__body">
        <div className="card__meta-row">
          <span>begin at</span>
          <span>{startsAt}</span>
        </div>
        <div className="card__meta-row">
          <span>ends at</span>
          <span>{endsAt} ({endsAtMinsLeft} min left)</span>
        </div>
      </div>
    );
  }

  if (status === 'ghost') {
    return (
      <div className="card__body">
        <div className="card__meta-row">
          <span>ends at</span>
          <span>{endsAt} ({endsAtMinsLeft} min left)</span>
        </div>
        <div className="card__meta-row">
          <span>empty for</span>
          <span>{emptyForMins} minutes</span>
        </div>
      </div>
    );
  }

  if (status === 'squatter') {
    return (
      <div className="card__body">
        <div className="card__meta-row">
          <span>booking</span>
          <span>—</span>
        </div>
        <div className="card__meta-row">
          <span>occupied for</span>
          <span>{occupiedForMins} minutes</span>
        </div>
      </div>
    );
  }

  return null;
}

export default function RoomCard({ room }) {
  const { name, floor, seats, email, status, activity, timeLabels, calendarId } = room;

  const calendarUrl = calendarId
    ? `https://calendar.google.com/calendar/r?cid=${encodeURIComponent(calendarId)}`
    : null;

  // Map API currentBooking fields to the flat props CardBody expects
  const flatRoom = {
    ...room,
    bookedBy:       room.currentBooking?.bookedBy ?? null,
    startsAt:       room.currentBooking?.startsAt ?? null,
    endsAt:         room.currentBooking?.endsAt ?? null,
    endsAtMinsLeft: room.currentBooking?.minsLeft ?? null,
  };

  return (
    <a
      className={`card card--${status}`}
      href={calendarUrl ?? '#'}
      target="_blank"
      rel="noreferrer"
      style={{ textDecoration: 'none', color: 'inherit' }}
    >
      <div className="card__header">
        <StatusBadge status={status} />
        <h3 className="card__name">{name}</h3>
        <p className="card__info">{floor} · {seats} seats · {email}</p>
      </div>
      <CardBody room={flatRoom} />
      <ActivityChart activity={activity} status={status} timeLabels={timeLabels} hasCamera={!!room.lastChecked} />
    </a>
  );
}
