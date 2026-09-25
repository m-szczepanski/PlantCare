import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function PlantCardSkeleton() {
  return (
    <Card className="flex h-full flex-col overflow-hidden">
      <Skeleton className="aspect-[16/7] w-full shrink-0 rounded-none" />
      <CardContent className="flex flex-1 flex-col gap-3 p-6">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 flex-1 space-y-2">
            <Skeleton className="h-6 w-2/3" />
            <Skeleton className="h-4 w-1/3" />
            <Skeleton className="h-4 w-1/2" />
          </div>
          <div className="space-y-1 text-right">
            <Skeleton className="ml-auto h-8 w-10" />
            <Skeleton className="ml-auto h-3 w-16" />
          </div>
        </div>
        <Skeleton className="h-5 w-24 rounded-full" />
        <div className="mt-auto space-y-2 border-t pt-3">
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      </CardContent>
    </Card>
  );
}

export function PlantCardSkeletonGrid({ count = 6, label }: { count?: number; label: string }) {
  return (
    <div role="status">
      <span className="sr-only">{label}</span>
      <ul className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3" aria-hidden="true">
        {Array.from({ length: count }, (_, index) => (
          <li key={index} className="h-full">
            <PlantCardSkeleton />
          </li>
        ))}
      </ul>
    </div>
  );
}