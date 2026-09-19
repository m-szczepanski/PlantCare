import { Navigate, Outlet, Route, Routes } from "react-router-dom";
import { AppShell } from "@/components/AppShell";
import { CalendarPage } from "@/pages/CalendarPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { InsightsPage } from "@/pages/InsightsPage";
import { PlantDetailPage } from "@/pages/PlantDetailPage";
import { PlantFormPage } from "@/pages/PlantFormPage";
import { PlantsPage } from "@/pages/PlantsPage";
import { WallPage } from "@/pages/WallPage";

function Layout() {
  return (
    <AppShell>
      <Outlet />
    </AppShell>
  );
}

export function App() {
  return (
    <Routes>
      <Route path="/wall" element={<WallPage />} />
      <Route element={<Layout />}>
        <Route index element={<DashboardPage />} />
        <Route path="/calendar" element={<CalendarPage />} />
        <Route path="/insights" element={<InsightsPage />} />
        <Route path="/plants" element={<PlantsPage />} />
        <Route path="/plants/new" element={<PlantFormPage />} />
        <Route path="/plants/:id" element={<PlantDetailPage />} />
        <Route path="/plants/:id/edit" element={<PlantFormPage />} />
        <Route path="*" element={<Navigate to="/plants" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
