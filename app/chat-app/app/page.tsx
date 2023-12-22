"use client"
import React, { useState, useEffect, ChangeEvent, KeyboardEvent } from 'react';
import Image from 'next/image';
import * as signalR from '@microsoft/signalr';
import { HttpTransportType } from '@microsoft/signalr';

interface User {
  _id: string;
  name: string;
  avatar: string;
}

interface Message {
  _id?: string;
  user_id: string;
  text: string;
  timestamp: number;
}

const Chat: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState<string>('');
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  useEffect(() => {
    // Initialize SignalR connection
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5044/hub/chat', {
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
      })
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (!connection) return;

    // Start SignalR connection
    connection.start().catch((err) => console.error('SignalR connection error:', err));

    // Subscribe to ReceiveMessage event
    connection.on('ReceiveMessage', (strMessage) => {
      // Handle incoming message
      const message: Message = JSON.parse(strMessage);

      setMessages((prevMessages) => {
        if (prevMessages.some(item => item.timestamp === message.timestamp)) { return prevMessages }
        return [...prevMessages, message]
      });
    });

    return () => {
      // Stop SignalR connection when component unmounts
      connection.stop();
    };
  }, [connection]);

  useEffect(() => {
    if (!connection || !selectedUser) return;

    // Notify server when a user is selected
    connection.invoke('SelectUser', selectedUser._id)
      .catch((err) => console.error('Error invoking SelectUser:', err));
    // eslint-disable-next-line no-use-before-define
  }, []);

  useEffect(() => {
    if (!connection) return;

    // Subscribe to UserSelected event
    connection.on('UserSelected', (selectedUserId) => {
      // Handle user selection in real-time
      const selected = users.find((user) => user._id === selectedUserId);
      if (selected) {
        setSelectedUser(selected);
      }
    });

    return () => {
      // Unsubscribe from UserSelected event when component unmounts
      connection.off('UserSelected');
    };
  }, []);

  useEffect(() => {
    // Fetch users from the API
    fetch('http://localhost:5044/api/chat/users')
      .then((response) => response.json())
      .then((data) => {
        setUsers(data);

        // Set selectedUser based on user_id from the URL
        const url = new URL(window.location.href);
        const user_id = url.searchParams.get('user_id');
        if (user_id) {
          const selected = data.find((user: User) => user._id === user_id);
          if (selected) {
            setSelectedUser(selected);
          }
        }
      })
      .catch((error) => console.error('Error fetching users:', error));
  }, []);

  useEffect(() => {
    if (selectedUser) {
      // Fetch messages from the API based on selectedUser
      fetch(`http://localhost:5044/api/chat/messages?user_id=${selectedUser._id}`)
        .then((response) => response.json())
        .then((data) => setMessages(data))
        .catch((error) => console.error('Error fetching messages:', error));
    }
  }, [selectedUser]);

  const sendMessage = (): void => {
    if (newMessage.trim() === '' || !selectedUser || !connection) return;

    const newMessageObj: Message = {
      user_id: selectedUser._id,
      text: newMessage,
      timestamp: Date.now(),
    };

    setNewMessage('');

    // Send message to the API
    fetch('http://localhost:5044/api/chat/sendMessage', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(newMessageObj),
    })
      .catch((error) => console.error('Error sending message:', error));
  };

  const selectUser = (user: User): void => {
    // Update the URL with the user_id
    const url = new URL(window.location.href);
    url.searchParams.set('user_id', user._id);
    window.history.pushState({}, '', url.toString());

    setSelectedUser(user);
  };

  const filteredMessages = selectedUser
    ? messages.filter((message) => message.user_id === selectedUser._id)
    : [];

  const handleKeyPress = (e: KeyboardEvent<HTMLInputElement>): void => {
    if (e.key === 'Enter') {
      sendMessage();
    }
  };

  return (
    <div className="min-h-screen flex">
      <div className="w-1/4 overflow-y-scroll p-4 border-r">
        <h2 className="text-xl font-semibold mb-4">Users</h2>
        <p className="mb-2">
          {selectedUser ? `Selected: ${selectedUser.name}` : 'No user selected'}
        </p>
        <ul>
          {users.map((user) => (
            <li
              key={user._id}
              className={`cursor-pointer mb-2 p-2 rounded ${selectedUser?._id === user._id ? 'bg-blue-200' : 'bg-gray-200'
                }`}
              onClick={() => selectUser(user)}
            >
              {user.name}
            </li>
          ))}
        </ul>
      </div>
      <div className="flex-1 p-4" style={{ maxHeight: '100vh', overflowY: 'scroll', display: 'flex', flexDirection: 'column' }}>
        {selectedUser && (
          <div className="border-b">
            <div className="w-12 h-12 rounded-full mb-2 overflow-hidden">
              {/* Use the Image component from next/image */}
              <Image
                src={selectedUser.avatar}
                alt={selectedUser.name}
                objectFit="cover"
                className="w-12 h-12 rounded-full mb-2"
                width={100}
                height={100}
              />
            </div>
            <p className="text-xl font-semibold">{selectedUser.name}</p>
          </div>
        )}
        <div className="flex-1 overflow-y-scroll">
          {filteredMessages.map((message, index) => (
            <div key={index} className="mb-2 p-2 rounded bg-gray-200">
              <p className="text-gray-800">{message.text}</p>
            </div>
          ))}
        </div>
        <div className="flex items-center">
          <input
            type="text"
            className="flex-1 border rounded p-2 mr-2"
            placeholder="Type a message..."
            value={newMessage}
            onChange={(e: ChangeEvent<HTMLInputElement>) =>
              setNewMessage(e.target.value)
            }
            onKeyPress={handleKeyPress}
          />
          <button
            className="bg-blue-500 text-white px-4 py-2 rounded"
            onClick={sendMessage}
          >
            Send
          </button>
        </div>
      </div>
    </div>
  );
};

export default Chat;