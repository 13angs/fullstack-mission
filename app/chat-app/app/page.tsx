"use client"
import React, { useState, useEffect, ChangeEvent, KeyboardEvent } from 'react';
import Image from 'next/image';

interface Member {
  _id: string;
  name: string;
  avatar: string;
}

interface Message {
  member_id: string;
  text: string;
  timestamp: number;
}

const Chat: React.FC = () => {
  const [members, setMembers] = useState<Member[]>([]);
  const [selectedMember, setSelectedMember] = useState<Member | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState<string>('');

  useEffect(() => {
    // Read member_id from the URL
    const url = new URL(window.location.href);
    const member_id = url.searchParams.get('member_id');

    // Fetch members from the API
    fetch('http://localhost:5044/api/chat/members')
      .then((response) => response.json())
      .then((data) => {
        setMembers(data);

        // Set selectedMember based on member_id from the URL
        if (member_id) {
          const selected = data.find((member: Member) => member._id === member_id);
          if (selected) {
            setSelectedMember(selected);
          }
        }
      })
      .catch((error) => console.error('Error fetching members:', error));
  }, []);

  useEffect(() => {
    // Fetch messages from the API based on selectedMember or member_id from the URL
    const idToFetch = selectedMember ? selectedMember._id : new URL(window.location.href).searchParams.get('member_id');
    if (idToFetch) {
      fetch(`http://localhost:5044/api/chat/messages?member_id=${idToFetch}`)
        .then((response) => response.json())
        .then((data) => setMessages(data))
        .catch((error) => console.error('Error fetching messages:', error));
    }
  }, [selectedMember]);

  const sendMessage = (): void => {
    if (newMessage.trim() === '' || !selectedMember) return;

    const newMessageObj: Message = {
      member_id: selectedMember._id,
      text: newMessage,
      timestamp: Date.now(),
    };

    // Update the UI immediately
    setMessages([...messages, newMessageObj]);
    setNewMessage('');

    // Send message to the API
    fetch('http://localhost:5044/api/chat/sendMessage', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        member_id: selectedMember._id,
        text: newMessage,
      }),
    })
      .catch((error) => console.error('Error sending message:', error));
  };

  const selectMember = (member: Member): void => {
    // Update the URL with the member_id
    const url = new URL(window.location.href);
    url.searchParams.set('member_id', member._id);
    window.history.pushState({}, '', url.toString());

    setSelectedMember(member);
  };

  const filteredMessages = selectedMember
    ? messages.filter((message) => message.member_id === selectedMember._id)
    : [];

  const handleKeyPress = (e: KeyboardEvent<HTMLInputElement>): void => {
    if (e.key === 'Enter') {
      sendMessage();
    }
  };

  return (
    <div className="min-h-screen flex">
      <div className="w-1/4 overflow-y-scroll p-4 border-r">
        <h2 className="text-xl font-semibold mb-4">Members</h2>
        <ul>
          {members.map((member) => (
            <li
              key={member._id}
              className={`cursor-pointer mb-2 p-2 rounded ${selectedMember?._id === member._id ? 'bg-blue-200' : 'bg-gray-200'
                }`}
              onClick={() => selectMember(member)}
            >
              {member.name}
            </li>
          ))}
        </ul>
      </div>
      <div className="flex-1 p-4" style={{ maxHeight: '100vh', overflowY: 'scroll', display: 'flex', flexDirection: 'column' }}>
        {selectedMember && (
          <div className="border-b">
            <div className="w-12 h-12 rounded-full mb-2 overflow-hidden">
              {/* Use the Image component from next/image */}
              <Image
                src={selectedMember.avatar}
                alt={selectedMember.name}
                objectFit="cover"
                className="w-12 h-12 rounded-full mb-2"
                width={100}
                height={100}
              />
            </div>
            <p className="text-xl font-semibold">{selectedMember.name}</p>
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