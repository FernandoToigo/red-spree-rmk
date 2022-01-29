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

For this specific game the frame is defined as:

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

Because there's only one entry point, it is easy to change the way the frame is defined. For example, we can simulate any frame time we want or mock the input if we need to. We can also decide whenever we want if we want to the FixedUpdate or Update.

Since the introduction of the new Input System we can now read user input from the FixedUpdate. This enables us to write game logic in this event, gaining the following advantages:

- Game logic and physics force changes can be done in the same frame function.
- Avoids problems with very high or very low frame times.
- Frees resources for better rendering performance.

While having the following disadvantages:

- Rendering frames without a game simulation in between is useless because there is no change between them, so the frames are identical. This can be solved by implementing an interpolation on the visuals between game states.
- If frames start to take more than the fixed time step to finish then the game is going to start falling. If the frames start to get faster then it can recover otherwise frames may be dropped.

### Automated Tests

We can create tests which pass through every system by creating a similar frame function from the real one and mocking the hardware input instead of gathering them from the real devices.

Furthermore, any system can be easily tested individually by calling its update function with the corresponding input and then analyzing the returned report and/or checking the changes in its internal state. An example of this type of test is implemented in the GameTests class of this project.

Because all the game logic is not tied to the Update event from Unity, these tests can be run as EditMode Tests. This means that tests that would otherwise require a lot of real time to finish are executed as fast as possible, making it feasible to execute them frequently as opposed to waiting for a CI process to be run.

### Performance

This architecture allows for any type of design to be used inside a system class. For example, the Game class could be constructed using a data oriented design or as ECS, which can leverage better use of the CPU cache than the usual Unity component system.

Moreover, this project contains two new types of containers which help to increase the overall performance of the game:

- ReusableArray: basically an encapsulation of an array and an integer which indicates how many elements of the array are set. It behaves very similarly to a List, but has fixed size which may help avoid unexpected heap allocations.

- ArrayLinkedList: doubly linked list implemented with an array. It has O(1) complexity for inserting, removal and random access. If the element type is a struct, the data also has the advantage of being laid out contiguously in memory. The disadvantage is its fixed size and higher memory footprint.
















