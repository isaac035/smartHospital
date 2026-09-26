import { useEffect, useMemo, useState } from 'react'
import { Calendar, dateFnsLocalizer } from 'react-big-calendar'
import format from 'date-fns/format'
import parse from 'date-fns/parse'
import startOfWeek from 'date-fns/startOfWeek'
import getDay from 'date-fns/getDay'
import addDays from 'date-fns/addDays'
import enUS from 'date-fns/locale/en-US'
import 'react-big-calendar/lib/css/react-big-calendar.css'
import DashboardLayout from '../../layouts/DashboardLayout'
import { doctorNavigation as navigation } from './doctorNavigation'
import { getMyDoctorProfile } from '../../services/doctorService'
import { listSchedules } from '../../services/scheduleService'

const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']

const locales = { 'en-US': enUS }
const localizer = dateFnsLocalizer({
  format,
  parse,
  startOfWeek: () => startOfWeek(new Date(), { weekStartsOn: 1 }),
  getDay,
  locales,
})

export default function MySchedule() {
  const [schedules, setSchedules] = useState([])
  const [currentDate, setCurrentDate] = useState(new Date())
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true)
        const profile = await getMyDoctorProfile()
        setSchedules(await listSchedules({ doctorId: profile.id }))
        setError('')
      } catch (requestError) {
        setError(requestError.response?.status === 404
          ? 'No doctor profile is linked to your account yet. Please contact an administrator.'
          : 'Unable to load your schedule.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const events = useMemo(() => {
    const weekStart = startOfWeek(currentDate, { weekStartsOn: 1 })
    return schedules.map((schedule) => {
      const dayIndex = DAY_NAMES.indexOf(schedule.dayOfWeek)
      const day = addDays(weekStart, dayIndex)
      const [startHour, startMinute] = schedule.startTime.split(':').map(Number)
      const [endHour, endMinute] = schedule.endTime.split(':').map(Number)
      const start = new Date(day); start.setHours(startHour, startMinute, 0, 0)
      const end = new Date(day); end.setHours(endHour, endMinute, 0, 0)
      return { id: schedule.id, title: `${schedule.consultationTypeName} (${schedule.startTime.slice(0, 5)}–${schedule.endTime.slice(0, 5)})`, start, end }
    })
  }, [schedules, currentDate])

  return <DashboardLayout role="Doctor" navigation={navigation} title="My Schedule" subtitle="Your recurring weekly availability, as set by hospital administration.">
    {error && <p className="form-error" role="alert">{error}</p>}
    {!loading && !error && schedules.length === 0 && <p className="empty-state">You have no availability sessions scheduled yet.</p>}

    {!error && <div className="panel calendar-page" style={{ padding: 16, height: 640 }}>
      <Calendar
        localizer={localizer}
        events={events}
        defaultView="week"
        views={['week']}
        date={currentDate}
        onNavigate={setCurrentDate}
        style={{ height: '100%' }}
      />
    </div>}
  </DashboardLayout>
}
