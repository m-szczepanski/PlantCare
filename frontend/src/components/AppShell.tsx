import type { ComponentType, ReactNode } from "react";
import { Armchair, BarChart3, BookOpen, CalendarDays, Leaf, LayoutDashboard, Plus, Sprout } from "lucide-react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { KeyboardShortcuts } from "@/components/KeyboardShortcuts";
import { ModeToggle } from "@/components/ModeToggle";
import { ScrollRestoration } from "@/components/ScrollRestoration";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar";

interface NavItem {
  to: string;
  label: string;
  icon: ComponentType<{ className?: string }>;
  end: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/calendar", label: "Calendar", icon: CalendarDays, end: true },
  { to: "/insights", label: "Insights", icon: BarChart3, end: true },
  { to: "/rooms", label: "Rooms", icon: Armchair, end: true },
  { to: "/profiles", label: "Profiles", icon: BookOpen, end: true },
  { to: "/plants", label: "Plants", icon: Sprout, end: false },
  { to: "/plants/new", label: "Add plant", icon: Plus, end: true },
];

function isNavItemActive({ to, end }: NavItem, pathname: string): boolean {
  return end ? pathname === to : pathname === to || pathname.startsWith(`${to}/`);
}

export function AppShell({ children }: { children: ReactNode }) {
  const { pathname } = useLocation();

  return (
    <SidebarProvider>
      <KeyboardShortcuts />
      <ScrollRestoration />
      <Sidebar collapsible="offcanvas">
        <SidebarHeader>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton size="lg" asChild>
                <Link to="/">
                  <Leaf className="text-primary" />
                  <span className="font-bold">PlantCare</span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupLabel>Navigation</SidebarGroupLabel>
            <SidebarMenu>
              {NAV_ITEMS.map((item) => (
                <SidebarMenuItem key={item.to}>
                  <SidebarMenuButton
                    asChild
                    isActive={isNavItemActive(item, pathname)}
                  >
                    <NavLink to={item.to} end={item.end}>
                      <item.icon />
                      <span>{item.label}</span>
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroup>
        </SidebarContent>
      </Sidebar>
      <SidebarInset>
        <header className="sticky top-0 z-10 flex h-14 shrink-0 items-center gap-2 border-b bg-background/95 px-4 backdrop-blur">
          <SidebarTrigger className="h-11 w-11 sm:h-8 sm:w-8" />
          <Link to="/" className="flex items-center gap-1.5 font-bold md:hidden">
            <Leaf className="h-4 w-4 text-primary" />
            PlantCare
          </Link>
          <div className="ml-auto">
            <ModeToggle />
          </div>
        </header>
        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-8 sm:px-6">
          {children}
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}
