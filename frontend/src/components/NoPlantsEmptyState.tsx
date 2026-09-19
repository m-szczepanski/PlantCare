import { Link } from "react-router-dom";
import { Sprout } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/EmptyState";

export function NoPlantsEmptyState() {
  return (
    <Card>
      <CardContent className="p-0">
        <EmptyState
          icon={Sprout}
          title="No plants yet"
          description="You have no plants yet. Add your first plant and PlantCare will keep track of watering for you."
          action={
            <Button asChild>
              <Link to="/plants/new">Add your first plant</Link>
            </Button>
          }
        />
      </CardContent>
    </Card>
  );
}
