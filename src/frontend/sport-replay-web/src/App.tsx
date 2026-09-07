import { Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './hooks/useAuth';
import { AppLayout } from './layouts/AppLayout';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { DashboardPage } from './pages/DashboardPage';
import { ClubsPage } from './pages/ClubsPage';
import { ClubDetailPage } from './pages/ClubDetailPage';
import { CourtsPage } from './pages/CourtsPage';
import { CamerasPage } from './pages/CamerasPage';
import { MatchesPage } from './pages/MatchesPage';
import { MatchDetailPage } from './pages/MatchDetailPage';
import { VideoPage } from './pages/VideoPage';
import { PaymentsPage } from './pages/PaymentsPage';
import { ProfilePage } from './pages/ProfilePage';
import { AdminPage } from './pages/AdminPage';
import { PlayerSearchPage } from './pages/PlayerSearchPage';
import { homePath, isStaff } from './utils/roles';

function Private({ children }: { children: JSX.Element }) {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return children;
}

function Staff({ children }: { children: JSX.Element }) {
  const { user } = useAuth();
  if (!isStaff(user?.role)) return <Navigate to="/buscar" replace />;
  return children;
}

function HomeRedirect() {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return <Navigate to={homePath(user.role)} replace />;
}

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          element={
            <Private>
              <AppLayout />
            </Private>
          }
        >
          <Route path="/buscar" element={<PlayerSearchPage />} />
          <Route path="/dashboard" element={<Staff><DashboardPage /></Staff>} />
          <Route path="/clubs" element={<Staff><ClubsPage /></Staff>} />
          <Route path="/clubs/:id" element={<Staff><ClubDetailPage /></Staff>} />
          <Route path="/courts" element={<Staff><CourtsPage /></Staff>} />
          <Route path="/cameras" element={<Staff><CamerasPage /></Staff>} />
          <Route path="/admin/cameras" element={<Staff><CamerasPage /></Staff>} />
          <Route path="/matches" element={<Staff><MatchesPage /></Staff>} />
          <Route path="/matches/:id" element={<MatchDetailPage />} />
          <Route path="/videos/:id" element={<VideoPage />} />
          <Route path="/payments" element={<PaymentsPage />} />
          <Route path="/payments/:id" element={<PaymentsPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/admin" element={<Staff><AdminPage /></Staff>} />
        </Route>
        <Route path="*" element={<HomeRedirect />} />
      </Routes>
    </AuthProvider>
  );
}
