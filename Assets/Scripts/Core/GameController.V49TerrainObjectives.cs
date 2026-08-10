// V49 compatibility shim retained so an already-installed V49 project does
// not leave a stale implementation behind. V50 builds and binds terrain areas
// explicitly from GameController.V50TerrainAreaBattlefield.cs.
public partial class GameController
{
    private void BindObjectivesToTerrainAreas11()
    {
        BuildAndBindStandardTerrainAreas50();
    }
}
