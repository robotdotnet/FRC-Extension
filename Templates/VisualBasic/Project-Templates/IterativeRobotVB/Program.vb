'DO NOT MODIFY THIS CLASS
'This class automatically calls your robot class. Please place all user
'code in the $safeprojectname$.vb class. Any changes made in here most likely
'will cause the program to not run.

Imports WPILib

Module Program

    Sub Main()
        RobotBase.Main(Nothing, GetType($safeprojectname$))
    End Sub

End Module
