Imports WPILib
Imports WPILib.SmartDashboard

''' <summary>
''' The VM is configured to automatically run this class, and to call the
''' functions corresponding to each mode, as described in the IterativeRobot
''' documentation. 
''' </summary>
Public Class $safeprojectname$
    Inherits IterativeRobot

    Const defaultAuto As String = "Default"
    Const customAuto As String = "My Auto"
    Dim autoSelected As String
    Dim chooser As SendableChooser

    ''' <summary>
    ''' This function is run when the robot is first started up and should be used
    ''' for any initialization code.
    ''' </summary>
    Public Overrides Sub RobotInit()
        chooser = New SendableChooser()
        chooser.AddDefault("Default Auto", defaultAuto)
        chooser.AddObject("My Auto", customAuto)
        SmartDashboard.PutData("Chooser", chooser)
    End Sub

    '' This autonomous (along with the sendable chooser above) shows how to select between
    '' different autonomous modes using the dashboard. The senable chooser code works with
    '' the Java SmartDashboard. If you prefer the LabVIEW Dashboard, remove all the chooser
    '' code an uncomment the GetString code to get the uto name from the text box below
    '' the gyro.
    '' You can add additional auto modes by adding additional comparisons to the switch
    '' structure below with additional strings. If using the SendableChooser
    '' be sure to add them to the chooser code above as well.
    Public Overrides Sub AutonomousInit()
        autoSelected = CType(chooser.GetSelected(), String)
        ''autoSelected = SmartDashboard.GetString("Auto Selector", defaultAuto)
        Console.WriteLine("Auto selected: " + autoSelected)
    End Sub

    ''' <summary>
    ''' This function is called periodically during autonomous
    ''' </summary>
    Public Overrides Sub AutonomousPeriodic()
        Select Case autoSelected
            Case customAuto
            '' Put custom auto code here
            Case Else
            '' put default auto code here
            
        End Select
            
    End Sub

    ''' <summary>
    ''' This function is called periodically during operator control
    ''' </summary>
    Public Overrides Sub TeleopPeriodic()

    End Sub

    ''' <summary>
    ''' This function is called periodically during test mode
    ''' </summary>
    Public Overrides Sub TestPeriodic()

    End Sub

End Class
