import { Route, Routes, Navigate } from 'react-router-dom';
import LandingPage from './pages/LandingPage';
import KnowledgeListPage from './pages/KnowledgeListPage';
import KnowledgeFormPage from './pages/KnowledgeFormPage';
import ChatPage from './pages/ChatPage';

function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/knowledge" element={<KnowledgeListPage />} />
      <Route path="/knowledge/new" element={<KnowledgeFormPage />} />
      <Route path="/knowledge/:id/edit" element={<KnowledgeFormPage />} />
      <Route path="/chat" element={<ChatPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
