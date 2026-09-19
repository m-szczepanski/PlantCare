import { useState } from "react";
import { Link } from "react-router-dom";
import { SearchX } from "lucide-react";
import { EmptyState } from "@/components/EmptyState";
import { NoPlantsEmptyState } from "@/components/NoPlantsEmptyState";
import { PlantCard } from "@/components/PlantCard";
import { PlantCardSkeletonGrid } from "@/components/PlantCardSkeleton";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { usePlants, useWaterPlant } from "@/hooks/usePlants";
import {
  dueFilterOptions,
  filterPlants,
  sortOptions,
  type DueFilter,
  type SortKey,
} from "@/lib/plantFilters";
import { touchButton, touchField } from "@/lib/ui";

export function PlantsPage() {
  const { data: plants, isPending, isError, error } = usePlants();
  const water = useWaterPlant();
  const [search, setSearch] = useState("");
  const [due, setDue] = useState<DueFilter>("all");
  const [sort, setSort] = useState<SortKey>("name");

  if (isPending) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Plants</h1>
        <PlantCardSkeletonGrid label="Loading plants..." />
      </div>
    );
  }

  if (isError) {
    return (
      <Card>
        <CardContent className="pt-6 text-destructive">
          Could not load plants: {(error as Error).message}
        </CardContent>
      </Card>
    );
  }

  if (!plants || plants.length === 0) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold">Plants</h1>
        <NoPlantsEmptyState />
      </div>
    );
  }

  const visible = filterPlants(plants, { search, due, sort });

  function clearFilters() {
    setSearch("");
    setDue("all");
    setSort("name");
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Plants</h1>
        <Button asChild className={touchButton}>
          <Link to="/plants/new">Add plant</Link>
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <Input
          aria-label="Search plants"
          placeholder="Search name, species, location..."
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          className={`w-full sm:w-64 ${touchField}`}
        />
        <Select value={due} onValueChange={(value) => setDue(value as DueFilter)}>
          <SelectTrigger aria-label="Filter by due status" className={touchField}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {dueFilterOptions.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={sort} onValueChange={(value) => setSort(value as SortKey)}>
          <SelectTrigger aria-label="Sort plants" className={touchField}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {sortOptions.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <span className="text-sm text-muted-foreground">
          {visible.length} of {plants.length}
        </span>
      </div>

      {visible.length === 0 ? (
        <Card>
          <CardContent className="p-0">
            <EmptyState
              icon={SearchX}
              title="No matching plants"
              description="No plants match the current search and filters."
              action={
                <Button variant="outline" onClick={clearFilters} className={touchButton}>
                  Clear filters
                </Button>
              }
            />
          </CardContent>
        </Card>
      ) : (
        <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {visible.map((plant) => (
            <li key={plant.id}>
              <PlantCard
                plant={plant}
                onWater={(p) => water.mutate({ id: p.id })}
                isWatering={water.isPending && water.variables?.id === plant.id}
              />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default PlantsPage;
