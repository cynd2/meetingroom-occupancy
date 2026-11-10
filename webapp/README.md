# Meeting Room Status Dashboard (webapp)

A React dashboard that shows, in real time, which meeting rooms are free, occupied, or flagged as a ghost. It is the frontend of the Meeting Room Occupancy Detection system. The backend (ASP.NET Core) combines Google Calendar bookings with camera-based person detection, and this app displays the result.

## Features

- Live room cards with a status badge, floor, seat count and room email
- Summary pills: total rooms, free rooms, occupied rooms
- Filter buttons: All, Free, Occupied
- A "last 15 min activity" chart per room, built from camera detections
- Live clock in Jakarta time (WIB), with data refreshed every 60 seconds
- Each card links to the room's Google Calendar
- Rooms without a camera show a "camera not connected" note instead of activity data

## Room statuses

The backend compares the Google Calendar booking with what the camera sees. A room is flagged as a **ghost** whenever the two disagree:

| Status                         | Calendar       | Camera              | Meaning                                  |
| ------------------------------ | -------------- | ------------------- | ---------------------------------------- |
| **Ghost (booked, empty)**      | Room is booked | Nobody in the room  | The booking is not being used            |
| **Ghost (in use, not booked)** | No booking     | Someone in the room | The room is being used without a booking |
| **Occupied**                   | Room is booked | Someone in the room | The booking is being used as planned     |
| **Free**                       | No booking     | Nobody in the room  | The room is available                    |

There is no separate "squatter" status. Someone using a room without a booking is shown as ghost.

Each card shows details that match its status, for example the next booking, the end time and minutes left, or how long the room has been in the ghost state.

## Tech stack

- React 19
- Vite 8 (dev server and build)
- Recharts (activity charts)
- ESLint

## Project structure

```
webapp/
├── index.html
├── vite.config.js            Dev server and /api proxy
├── package.json
├── public/                   favicon and icons
└── src/
    ├── main.jsx              App entry point
    ├── App.jsx               Fetches /api/rooms, polls every 60 s, holds filter state
    ├── index.css, App.css
    └── components/
        ├── TopBar/           Title, live indicator, summary pills (StatPill)
        ├── FilterBar/        All / Free / Occupied buttons and the clock
        ├── FloorSection/     Grid of room cards, applies the filter
        └── RoomCard/         Card, StatusBadge and ActivityChart
```

`frame-13-structure.json` is a Figma export of the dashboard design, kept as a design reference.

## Getting started

### Requirements

- Node.js (a current LTS version) and npm
- The backend API running on `http://localhost:5000`

### Install and run

```bash
cd webapp
npm install
npm run dev
```

Open the address Vite prints (usually `http://localhost:5173`).

### Other scripts

| Command | What it does |
|---------|--------------|
| `npm run dev` | Starts the dev server with hot reload |
| `npm run build` | Builds the production app into `dist/` |
| `npm run preview` | Serves the production build locally |
| `npm run lint` | Runs ESLint |

## Backend connection

The app calls `GET /api/rooms`. In development, `vite.config.js` forwards every `/api` request to `http://localhost:5000`

```json
{
  "summary": { "total": 0, "free": 0, "occupied": 0 },
  "floors": [
    {
      "label": "2nd Floor",
      "rooms": [
        {
          "id": 1,
          "name": "Room name",
          "floor": "2nd Floor",
          "seats": 4,
          "email": "room@example.com",
          "status": "free",
          "calendarId": "…@group.calendar.google.com",
          "currentBooking": { "bookedBy": "", "startsAt": "", "endsAt": "", "minsLeft": 0 },
          "nextBooking": { "range": "", "beginAt": "", "minsLeft": 0 },
          "lastChecked": "",
          "activity": [{ "t": "14.05", "v": 0 }],
          "timeLabels": []
        }
      ]
    }
  ]
}
```

If the server can't be reached, the page shows "Could not reach the server" with the error.


