import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function PlantCardSkeleton() {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="flex items-start justify-between gap-2">
          <Skeleton className="h-6 w-1/2" />
          <Skeleton className="h-5 w-20 rounded-full" />
        </div>
        <Skeleton className="mt-3 h-4 w-1/3" />
        <Skeleton className="mt-2 h-4 w-2/5" />
      </CardContent>
    </Card>
  );
}

export function PlantCardSkeletonGrid({ count = 6, label }: { count?: number; label: string }) {
  return (
    <div role="status">
      <span className="sr-only">{label}</span>
      <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3" aria-hidden="true">
        {Array.from({ length: count }, (_, index) => (
          <li key={index}>
            <PlantCardSkeleton />
          </li>
        ))}
      </ul>
    </div>
  );
}
