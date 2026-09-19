import { Navigate, Outlet, Route, Routes } from "react-router-dom";
import { AppShell } from "@/components/AppShell";
import { DashboardPage } from "@/pages/DashboardPage";
import { PlantDetailPage } from "@/pages/PlantDetailPage";
import { PlantFormPage } from "@/pages/PlantFormPage";
import { PlantsPage } from "@/pages/PlantsPage";

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
      <Route element={<Layout />}>
        <Route index element={<DashboardPage />} />
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
