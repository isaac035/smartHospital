import { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './useAuth';

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
      .withUrl('http://localhost:5100/hubs/hospital')
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, [user]);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          setStatus('Connected');
          Object.keys(eventsRef.current).forEach((eventName) => {
            connection.on(eventName, (...args) => {
              if (eventsRef.current[eventName]) {
                eventsRef.current[eventName](...args);
              }
            });
          });
        })
        .catch(e => console.log('Connection failed: ', e));

      connection.onreconnecting(() => setStatus('Reconnecting'));
      connection.onreconnected(() => setStatus('Connected'));
      connection.onclose(() => setStatus('Disconnected'));

      return () => {
        Object.keys(eventsRef.current).forEach((eventName) => {
          connection.off(eventName);
        });
        connection.stop();
      };
    }
  }, [connection]);

  return { connection, status };
};
