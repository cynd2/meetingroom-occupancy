import { AreaChart, Area, XAxis, YAxis, ResponsiveContainer } from 'recharts';
import './ActivityChart.css';

const CHART_COLOR = {
  free:     '#5D8623',
  ghost:    '#5D8623',
  occupied: '#B02626',
  squatter: '#B02626',
};

function makeZeroData() {
  const now = new Date();
  return Array.from({ length: 15 }, (_, i) => {
    const t = new Date(now.getTime() + 7 * 3600_000 - (14 - i) * 60_000);
    const hh = String(t.getUTCHours()).padStart(2, '0');
    const mm = String(t.getUTCMinutes()).padStart(2, '0');
    return { t: `${hh}.${mm}`, v: 0 };
  });
}

export default function ActivityChart({ activity, status, timeLabels, hasCamera }) {
  const color = CHART_COLOR[status] ?? '#5D8623';

  const data = activity?.length ? activity : makeZeroData();
  const noCamera = !activity?.length && !hasCamera;

  return (
    <div className="activity-chart">
      <span className="activity-chart__label">last 15 min activity</span>
      <div className="activity-chart__area" style={{ position: 'relative' }}>
        <ResponsiveContainer width="100%" height={36}>
          <AreaChart data={data} margin={{ top: 4, right: 0, left: 0, bottom: 0 }}>
            <XAxis dataKey="t" hide />
            <YAxis hide domain={[0, 1]} />
            <Area
              type="linear"
              dataKey="v"
              stroke={noCamera ? '#ccc' : color}
              strokeWidth={2.5}
              fill={noCamera ? '#ccc' : color}
              fillOpacity={0.25}
              dot={{ r: 3, fill: noCamera ? '#ccc' : '#000', strokeWidth: 0 }}
              isAnimationActive={false}
            />
          </AreaChart>
        </ResponsiveContainer>
        {noCamera && (
          <span className="activity-chart__no-camera">camera not connected</span>
        )}
      </div>
      {timeLabels?.length > 0 && (
        <div className="activity-chart__times">
          {timeLabels.map((t, i) => (
            <span key={i}>{t}</span>
          ))}
        </div>
      )}
    </div>
  );
}
