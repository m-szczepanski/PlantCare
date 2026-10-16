import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Cpu, Droplets, Gauge, Sprout } from "lucide-react";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useCreateDevice,
  useDeleteDevice,
  useDevices,
  useDevicesEnabled,
  useUpdateDevice,
} from "@/hooks/useDevices";
import type { CommandStyle, Device, DeviceType } from "@/api/types";
import { cn } from "@/lib/utils";
import { touchButton, touchField } from "@/lib/ui";

const DEVICE_TYPES: DeviceType[] = ["Valve", "Pump", "Sensor"];
const COMMAND_STYLES: CommandStyle[] = ["Pulse", "OnOff", "Tasmota"];

export function DevicesPage() {
  const { t } = useTranslation();
  const enabled = useDevicesEnabled();
  const { data: devices, isPending } = useDevices(enabled);
  const create = useCreateDevice();
  const [name, setName] = useState("");
  const [type, setType] = useState<DeviceType>("Valve");

  if (!enabled) {
    return (
      <div className="space-y-4">
        <Breadcrumbs items={[{ label: t("common.home"), to: "/" }, { label: t("devices.title") }]} />
        <h1 className="text-3xl font-bold">{t("devices.title")}</h1>
        <Card>
          <CardContent className="flex flex-col items-center px-6 py-10 text-center">
            <div className="rounded-full bg-muted p-3" aria-hidden="true">
              <Cpu className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mt-3 text-lg font-semibold">{t("devices.disabledTitle")}</h3>
            <p className="mt-1 max-w-sm text-sm text-muted-foreground">{t("devices.disabledDescription")}</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  function handleCreate() {
    const trimmed = name.trim();
    if (trimmed === "") return;
    create.mutate({ name: trimmed, type }, { onSuccess: () => setName("") });
  }

  return (
    <div className="space-y-4">
      <Breadcrumbs items={[{ label: t("common.home"), to: "/" }, { label: t("devices.title") }]} />
      <h1 className="text-3xl font-bold">{t("devices.title")}</h1>

      <div className="flex flex-wrap items-end gap-2">
        <div className="w-full space-y-1 sm:w-64">
          <Label htmlFor="deviceName">{t("device.name")}</Label>
          <Input
            id="deviceName"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder={t("devices.namePlaceholder")}
            className={touchField}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="deviceType">{t("device.typeLabel")}</Label>
          <Select value={type} onValueChange={(value) => setType(value as DeviceType)}>
            <SelectTrigger id="deviceType" className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {DEVICE_TYPES.map((option) => (
                <SelectItem key={option} value={option}>
                  {t(`device.type.${option}`)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button onClick={handleCreate} disabled={name.trim() === "" || create.isPending} className={touchButton}>
          {create.isPending ? t("common.adding") : t("devices.add")}
        </Button>
      </div>

      {isPending ? (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-48" />
          ))}
        </div>
      ) : !devices || devices.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center px-6 py-10 text-center">
            <div className="rounded-full bg-muted p-3" aria-hidden="true">
              <Cpu className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mt-3 text-lg font-semibold">{t("devices.emptyTitle")}</h3>
            <p className="mt-1 max-w-sm text-sm text-muted-foreground">{t("devices.emptyDescription")}</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {devices.map((device) => (
            <DeviceCard key={device.id} device={device} />
          ))}
        </div>
      )}
    </div>
  );
}

function DeviceCard({ device }: { device: Device }) {
  const { t } = useTranslation();
  const update = useUpdateDevice();
  const remove = useDeleteDevice();
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(device.name);
  const [type, setType] = useState<DeviceType>(device.type);
  const [commandStyle, setCommandStyle] = useState<CommandStyle>(device.commandStyle);
  const [commandTopic, setCommandTopic] = useState(device.commandTopic ?? "");
  const [stateTopic, setStateTopic] = useState(device.stateTopic ?? "");

  function syncFromDevice() {
    setName(device.name);
    setType(device.type);
    setCommandStyle(device.commandStyle);
    setCommandTopic(device.commandTopic ?? "");
    setStateTopic(device.stateTopic ?? "");
  }

  function save() {
    update.mutate(
      {
        id: device.id,
        input: {
          name: name.trim(),
          type,
          commandStyle,
          commandTopic: commandTopic.trim() || null,
          stateTopic: stateTopic.trim() || null,
        },
      },
      { onSuccess: () => setEditing(false) },
    );
  }

  return (
    <Card className="flex h-full flex-col overflow-hidden">
      <CardHeader className="pb-3">
        <CardTitle className="text-xl">{device.name}</CardTitle>
        <p className="text-sm text-muted-foreground">
          {t(`device.type.${device.type}`)} · {t(`device.commandStyle.${device.commandStyle}`)}
        </p>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-3 text-sm">
        {!editing ? (
          <>
            <div className="grid grid-cols-3 divide-x rounded-lg border bg-muted/40 text-center">
              <div className="px-2 py-3">
                <div className="flex items-center justify-center gap-1 text-2xl font-bold leading-none">
                  <Droplets className="h-4 w-4" aria-hidden="true" />
                  {device.lastMoisturePct === null ? "—" : `${device.lastMoisturePct}%`}
                </div>
                <div className="mt-1 text-xs text-muted-foreground">{t("device.moistureLabel")}</div>
              </div>
              <div className="px-2 py-3">
                <div className="flex items-center justify-center gap-1 text-2xl font-bold leading-none">
                  <Gauge className="h-4 w-4" aria-hidden="true" />
                  {device.waterLevel ? t(`device.waterLevel.${device.waterLevel}`) : "—"}
                </div>
                <div className="mt-1 text-xs text-muted-foreground">{t("device.waterLevelLabel")}</div>
              </div>
              <div className="px-2 py-3">
                <div className="flex items-center justify-center gap-1 text-2xl font-bold leading-none">
                  <Sprout className="h-4 w-4" aria-hidden="true" />
                  {device.plantCount}
                </div>
                <div className="mt-1 text-xs text-muted-foreground">{t("device.plants")}</div>
              </div>
            </div>
            <p className="text-xs text-muted-foreground">
              {t("device.mqttTopic", { topic: device.commandTopic ?? `${t("device.defaultTopic")}${device.id}/command` })}
            </p>
          </>
        ) : (
          <div className="space-y-3">
            <div className="space-y-1">
              <Label htmlFor={`device-name-${device.id}`}>{t("device.name")}</Label>
              <Input id={`device-name-${device.id}`} value={name} onChange={(event) => setName(event.target.value)} className={touchField} />
            </div>
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1">
                <Label htmlFor={`device-type-${device.id}`}>{t("device.typeLabel")}</Label>
                <Select value={type} onValueChange={(value) => setType(value as DeviceType)}>
                  <SelectTrigger id={`device-type-${device.id}`} className={touchField}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {DEVICE_TYPES.map((option) => (
                      <SelectItem key={option} value={option}>{t(`device.type.${option}`)}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label htmlFor={`device-style-${device.id}`}>{t("device.commandStyleLabel")}</Label>
                <Select value={commandStyle} onValueChange={(value) => setCommandStyle(value as CommandStyle)}>
                  <SelectTrigger id={`device-style-${device.id}`} className={touchField}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {COMMAND_STYLES.map((option) => (
                      <SelectItem key={option} value={option}>{t(`device.commandStyle.${option}`)}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-1">
              <Label htmlFor={`device-command-${device.id}`}>{t("device.commandTopic")}</Label>
              <Input id={`device-command-${device.id}`} value={commandTopic} onChange={(event) => setCommandTopic(event.target.value)} placeholder={`plantcare/${device.id}/command`} className={touchField} />
            </div>
            <div className="space-y-1">
              <Label htmlFor={`device-state-${device.id}`}>{t("device.stateTopic")}</Label>
              <Input id={`device-state-${device.id}`} value={stateTopic} onChange={(event) => setStateTopic(event.target.value)} placeholder={`plantcare/${device.id}/state`} className={touchField} />
            </div>
          </div>
        )}
        <div className={cn("flex flex-wrap gap-2", !editing && "mt-auto border-t pt-3")}>
          {editing ? (
            <>
              <Button size="sm" disabled={update.isPending || name.trim() === ""} onClick={save}>
                {update.isPending ? t("common.saving") : t("common.save")}
              </Button>
              <Button size="sm" variant="ghost" disabled={update.isPending} onClick={() => { syncFromDevice(); setEditing(false); }}>
                {t("common.cancel")}
              </Button>
            </>
          ) : (
            <Button size="sm" variant="outline" disabled={remove.isPending} onClick={() => { syncFromDevice(); setEditing(true); }}>
              {t("common.edit")}
            </Button>
          )}
          <AlertDialog open={confirmDelete} onOpenChange={setConfirmDelete}>
            <AlertDialogTrigger asChild>
              <Button size="sm" variant="outline" disabled={remove.isPending}>
                {t("common.delete")}
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t("devices.deleteTitle", { name: device.name })}</AlertDialogTitle>
                <AlertDialogDescription>{t("devices.deleteDescription")}</AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>{t("common.cancel")}</AlertDialogCancel>
                <AlertDialogAction
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  onClick={() => {
                    setConfirmDelete(false);
                    remove.mutate(device.id);
                  }}
                >
                  {t("common.delete")}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </CardContent>
    </Card>
  );
}

export default DevicesPage;
