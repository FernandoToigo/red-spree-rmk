# Red Spree Remake

The purpose of this project is to serve as an example of a different architecture for Unity projects which focuses on simplicity, productivity and performance. It uses predominantly structs and static classes while avoiding the default component architecture from Unity and OOP (reasons for those are explained at the end of this document).

### Main Component

All game logic derives from the Update of a single MonoBehaviour called Main. This component is alive throughout the whole lifetime of the game and is responsible for calling the Update function from all the other classes (which are plain old static classes). This means that we can control the order of execution of the different systems without needing to worry about the script execution order from Unity.

The Main class also manages the transition of states of the game such as splash, loading, main menu, changing of levels, and so forth. This component is contained in its own scene which is never unloaded and the transition of states is done by loading other scenes additively. This means that the Update function is always running and that there is no need for using the DontDestroyOnLoad feature.

### System Classes

Despite avoiding OOP we still want to maintain a certain level of abstraction. This is done by separating the different systems into specific classes which for the most part can work independently. In general, each of these classes contain the public functions Initialize and Update. More public functions can be added but that should be an exception in order to avoid the biggest cognitive challenge when trying to understand static classes: that there are usually too many external functions changing its state at random locations.

These system classes also usually contains a public State field which holds data that persists over frames. The idea here is that only the class itself can change its state by means of calls to its public functions and therefore external accesses of this State field should be done exclusively for reading (this is not enforced by any type of encapsulation as to avoid unnecessary copies, additional code and to boost productivity).

An update function usually has the following signature:

```csharp
public static State State;

public static Report Update(Input input)
{
   // Changes State based on Input and return a Report containing what has changed.
}
```
Where Input is a struct containing what needs to be executed in this system and the returned Report is another struct containing what has changed. This report struct is then spread throughout the other systems, replacing the functionality which would be accomplished by invoking events or using the observable pattern.

Also, usage of callbacks is avoided and replaced by polling methods when possible. This makes the code easier to understand as a whole because there are never surprises on the call stack of function calls.

### Frame

In this project a frame is defined as:

```csharp
    public void FixedUpdate()
    {
        var hardInput = GetHardInput();                // (1)
        var gameInput = new Game.Input();              // (2)
        var time = GetFrameTime();                     // (3)

        UserInterface.Update(ref gameInput);           // (4)
        GameInput.Update(ref gameInput, hardInput);    // (5)
        var gameReport = Game.Update(gameInput, time); // (6)
        UserInterface.Render(gameReport, time);        // (7)
        GameAudio.Update(gameReport);                  // (8)
    }
```

(1) Gathers hardware input from the devices.  
(2) Creates a struct which contains commands for the game to execute.  
(3) Creates a struct containing the delta time for this frame.  
(4) The UserInterface updates by changing the game input if necessary. i.e. when the button to start the game is pressed.  
(5) The GameInput updates by transforming hardware input into game input.  
(6) The Game simulation is updated based on the input, generating a report containing change information.  
(7) The Game report is passed to the UserInterface to update data on screen or to start animations.  
(8) The Game report is passed to the GameAudio to start playing sound effects.