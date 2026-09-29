import { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './useAuth';
import { getStoredAuth } from '../utils/auth';

export const useSignalR = (events = {}) => {
  const [connection, setConnection] = useState(null);
  const [status, setStatus] = useState('Disconnected');
  const { user } = useAuth();
  
  // Keep the latest events in a ref so we don't re-bind on every render
  const eventsRef = useRef(events);
  useEffect(() => {
    eventsRef.current = events;
  }, [events]);

  useEffect(() => {
    if (!user) return;

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5100/hubs/hospital', {
        accessTokenFactory: () => getStoredAuth()?.token || '',
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, [user]);

  useEffect(() => {
    if (connection) {
      let disposed = false
      let retryTimer
      Object.keys(eventsRef.current).forEach((eventName) => {
        connection.on(eventName, (...args) => {
          if (eventsRef.current[eventName]) eventsRef.current[eventName](...args)
        })
      })
      const start = async () => {
        try {
          await connection.start()
          if (!disposed) setStatus('Connected')
        } catch {
          if (!disposed) {
            setStatus('Disconnected')
            retryTimer = setTimeout(start, 3000)
          }
        }
      }
      start()

      connection.onreconnecting(() => setStatus('Reconnecting'));
      connection.onreconnected(() => setStatus('Connected'));
      connection.onclose(() => setStatus('Disconnected'));

      return () => {
        disposed = true
        clearTimeout(retryTimer)
        Object.keys(eventsRef.current).forEach((eventName) => {
          connection.off(eventName);
        });
        connection.stop();
      };
    }
  }, [connection]);

  return { connection, status };
};
