"use client"
import { useState } from 'react';

const Chat = () => {
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');

  const sendMessage = () => {
    if (newMessage.trim() === '') return;

    setMessages([...messages, { text: newMessage, timestamp: Date.now() }]);
    setNewMessage('');
  };

  return (
    <div className="min-h-screen flex flex-col">
      <div className="flex-1 overflow-y-scroll p-4">
        {messages.map((message, index) => (
          <div
            key={index}
            className="mb-2 p-2 rounded bg-gray-200"
          >
            <p className="text-gray-800">{message.text}</p>
          </div>
        ))}
      </div>
      <div className="p-4 flex items-center">
        <input
          type="text"
          className="flex-1 border rounded p-2 mr-2"
          placeholder="Type a message..."
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
        />
        <button
          className="bg-blue-500 text-white px-4 py-2 rounded"
          onClick={sendMessage}
        >
          Send
        </button>
      </div>
    </div>
  );
};

export default Chat;
