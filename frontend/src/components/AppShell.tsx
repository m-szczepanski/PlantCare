import type { ComponentType, ReactNode } from "react";
import { Activity, Armchair, BarChart3, BookOpen, CalendarDays, Leaf, LayoutDashboard, Plus, Sprout } from "lucide-react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useQueryClient } from "@tanstack/react-query";
import i18n from "@/i18n";
import { useEffect } from "react";
import { KeyboardShortcuts } from "@/components/KeyboardShortcuts";
import { LanguageToggle } from "@/components/LanguageToggle";
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
  labelKey: string;
  icon: ComponentType<{ className?: string }>;
  end: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { to: "/", labelKey: "nav.dashboard", icon: LayoutDashboard, end: true },
  { to: "/calendar", labelKey: "nav.calendar", icon: CalendarDays, end: true },
  { to: "/insights", labelKey: "nav.insights", icon: BarChart3, end: true },
  { to: "/rooms", labelKey: "nav.rooms", icon: Armchair, end: true },
  { to: "/profiles", labelKey: "nav.profiles", icon: BookOpen, end: true },
  { to: "/status", labelKey: "nav.status", icon: Activity, end: true },
  { to: "/plants", labelKey: "nav.plants", icon: Sprout, end: false },
  { to: "/plants/new", labelKey: "nav.addPlant", icon: Plus, end: true },
];

function isNavItemActive({ to, end }: NavItem, pathname: string): boolean {
  return end ? pathname === to : pathname === to || pathname.startsWith(`${to}/`);
}

export function AppShell({ children }: { children: ReactNode }) {
  const { pathname } = useLocation();
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  useEffect(() => {
    const refetch = () => void queryClient.invalidateQueries();
    i18n.on("languageChanged", refetch);
    return () => {
      i18n.off("languageChanged", refetch);
    };
  }, [queryClient]);

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
            <SidebarGroupLabel>{t("nav.group")}</SidebarGroupLabel>
            <SidebarMenu>
              {NAV_ITEMS.map((item) => (
                <SidebarMenuItem key={item.to}>
                  <SidebarMenuButton
                    asChild
                    isActive={isNavItemActive(item, pathname)}
                  >
                    <NavLink to={item.to} end={item.end}>
                      <item.icon />
                      <span>{t(item.labelKey)}</span>
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroup>
        </SidebarContent>
      </Sidebar>
      <SidebarInset>
        <header className="sticky top-0 z-10 flex h-16 shrink-0 items-center gap-2 border-b bg-background/95 px-4 backdrop-blur sm:px-6">
          <SidebarTrigger className="h-11 w-11 sm:h-9 sm:w-9" aria-label={t("shell.toggleSidebar")} />
          <Link to="/" className="flex items-center gap-1.5 text-lg font-bold md:hidden">
            <Leaf className="h-5 w-5 text-primary" />
            PlantCare
          </Link>
          <div className="ml-auto flex items-center gap-1">
            <LanguageToggle />
            <ModeToggle />
          </div>
        </header>
        <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-8 sm:px-6 lg:px-8">
          {children}
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}
