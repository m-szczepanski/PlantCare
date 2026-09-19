import { Link, Navigate, Outlet, Route, Routes } from "react-router-dom";
import { ModeToggle } from "@/components/ModeToggle";
import { DashboardPage } from "@/pages/DashboardPage";
import { PlantDetailPage } from "@/pages/PlantDetailPage";
import { PlantFormPage } from "@/pages/PlantFormPage";
import { PlantsPage } from "@/pages/PlantsPage";

function Layout() {
  return (
    <div className="min-h-screen">
      <header className="border-b">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Link to="/" className="text-lg font-bold">
            PlantCare
          </Link>
          <div className="flex items-center gap-4">
            <nav className="flex gap-4 text-sm">
              <Link to="/" className="text-muted-foreground hover:text-foreground">
                Dashboard
              </Link>
              <Link to="/plants" className="text-muted-foreground hover:text-foreground">
                Plants
              </Link>
              <Link to="/plants/new" className="text-muted-foreground hover:text-foreground">
                Add plant
              </Link>
            </nav>
            <ModeToggle />
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-6 py-8">
        <Outlet />
      </main>
    </div>
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
