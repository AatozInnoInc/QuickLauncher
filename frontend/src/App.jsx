import React, { useState } from 'react';

function App() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);

  const handleChange = async (e) => {
    const q = e.target.value;
    setQuery(q);
    if (q) {
      try {
        const response = await fetch(`http://localhost:8000/search?q=${encodeURIComponent(q)}`);
        if (response.ok) {
          const data = await response.json();
          const items = Array.isArray(data?.results) ? data.results : Array.isArray(data) ? data : [];
          setResults(items);
        } else {
          console.error('Search failed');
        }
      } catch (error) {
        console.error('Error during search', error);
      }
    } else {
      setResults([]);
    }
  };

  const handleLaunch = async (path) => {
    try {
      await fetch(`http://localhost:8000/launch?path=${encodeURIComponent(path)}`, {
        method: 'POST',
      });
    } catch (error) {
      console.error('Error launching file', error);
    }
  };

  return (
    <div style={{ padding: '1rem', fontFamily: 'Arial, sans-serif' }}>
      <input
        type="text"
        value={query}
        onChange={handleChange}
        placeholder="Type a command or search query..."
        style={{ width: '100%', padding: '0.5rem', fontSize: '1rem', marginBottom: '1rem' }}
      />
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {results.map((item, index) => (
          <li
            key={index}
            onClick={() => item?.path && handleLaunch(item.path)}
            style={{
              padding: '0.5rem',
              marginBottom: '0.25rem',
              cursor: 'pointer',
              border: '1px solid #ccc',
              borderRadius: '4px',
            }}
          >
            {item?.title || String(item)}
          </li>
        ))}
      </ul>
    </div>
  );
}

export default App;