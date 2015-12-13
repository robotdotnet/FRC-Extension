using HAL.Simulator;
using WPILib;

namespace $safeprojectname$
{
    public static class Program
{
    public static void Main()
    {
        RobotBase.Main(null, typeof($robotnamespace$.$robotclass$));
    }
}

public class Simulator : ISimulator
{
    public void Initialize()
    {
    }

    public void Start()
    {
        SimHooks.WaitForProgramStart();
        DriverStationGUI.DriverStation.StartDriverStationGui();
        using (var game = new $safeprojectname$())
                game.Run();
    }

    public string Name => "Mono Game Simulator";
}
}