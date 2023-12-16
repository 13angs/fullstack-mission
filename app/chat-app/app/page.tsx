"use client"
import React, { useState, ChangeEvent, KeyboardEvent } from 'react';
import Image from 'next/image'; // Import the Image component from next/image

interface Member {
  id: number;
  name: string;
  avatar: string;
}

interface Message {
  memberId: number;
  text: string;
  timestamp: number;
}

const Chat: React.FC = () => {
  const [members, setMembers] = useState<Member[]>([
    { id: 1, name: 'Member 1', avatar: 'https://cdn.dribbble.com/users/3841177/screenshots/11950347/cartoon-avatar_2020__8_circle.png' },
    { id: 2, name: 'Member 2', avatar: 'https://www.gamer-hub.io/static/img/team/sam.png' },
    // Add more members as needed
  ]);

  const [selectedMember, setSelectedMember] = useState<Member | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState<string>('');

  const sendMessage = (): void => {
    if (newMessage.trim() === '' || !selectedMember) return;

    setMessages([
      ...messages,
      { memberId: selectedMember.id, text: newMessage, timestamp: Date.now() },
    ]);
    setNewMessage('');
  };

  const selectMember = (member: Member): void => {
    setSelectedMember(member);
  };

  const filteredMessages = selectedMember
    ? messages.filter((message) => message.memberId === selectedMember.id)
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
              key={member.id}
              className={`cursor-pointer mb-2 p-2 rounded ${
                selectedMember?.id === member.id ? 'bg-blue-200' : 'bg-gray-200'
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




