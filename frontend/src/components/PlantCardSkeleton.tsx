import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function PlantCardSkeleton() {
  return (
    <Card>
      <CardContent className="flex items-start gap-4 p-6">
        <Skeleton className="h-14 w-14 shrink-0" />
        <div className="min-w-0 flex-1">
          <Skeleton className="h-6 w-1/2" />
          <Skeleton className="mt-3 h-4 w-1/3" />
          <Skeleton className="mt-3 h-5 w-40" />
        </div>
        <div className="shrink-0 space-y-1 text-right">
          <Skeleton className="ml-auto h-7 w-10" />
          <Skeleton className="ml-auto h-3 w-16" />
        </div>
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
