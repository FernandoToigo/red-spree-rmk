# Red Spree Remake

The purpose of this project is to present an example of my current vision of the ideal architecture for Unity projects. This architecture follows the principles of simplicity, productivity, performance, and scalability. It employs for the most part structs and static classes while avoiding the default component architecture from Unity and OOP (reasons are detailed [here](#design-decisions)).

A Web version of the game can be played [here](https://fernandotoigo.github.io/red-spree-rmk/).

### Main Component

All game logic derives in some way from a single instance of a MonoBehaviour called Main. This component is alive throughout the lifetime of the game and is responsible for calling the Update function from all the system classes (which are plain old static classes). This gives us the capacity to control the order of execution of the different systems without needing to worry about the execution order from Unity scripts.

The Main class also manages the transition of the various states of the game such as splash, loading, main menu, changing of levels, and so forth. It does that by being contained in its own scene which is never unloaded. This means that the Update function is always running and that the transition between the states of the game is done by loading their respective scenes additively.

### System Classes

Despite avoiding OOP we still want to maintain a certain level of independence between the classes. This is done by separating the different systems into their own classes which, for the most part, can work independently. In general, each of these classes contains only the Initialize and Update functions as public with side effects. Adding more of those types of functions is allowed but discouraged to avoid the biggest cognitive challenge faced when trying to understand static classes: when there are too many external functions changing their state at random locations.

System classes also usually contain a public State field that holds data that persists over frames. The intent is that only the class itself can change its state through calls to its public functions and, as such, external accesses to this State field should be done exclusively for reading (this is not enforced by any type of encapsulation to avoid unnecessary copies, additional code and to boost productivity).

An update function usually follows the pattern:

```csharp
public static State State;

public static Report Update(Input input)
{
   // Changes the State based on Input.
   // Returns a Report describing the changes, if necessary.
}
```
Where Input is a struct containing what needs to be executed/changed in this system and the returned Report is another struct containing what has been changed in this frame. The report struct is then spread throughout the other systems, replacing the functionality which would be accomplished by invoking events or using the observable pattern, for example.

Speaking of events, usage of callbacks is avoided and replaced by polling methods whenever possible. This makes the code easier to understand as a whole because there are never surprises on the call stack of function calls.

### Frame

For this specific game the update/frame function of Main is defined as:

```csharp
private static void GameplayFrame(FrameTime time, HardInput hardInput) // (1)
{
    var gameInput = new Game.Input();              // (2)
    
    UserInterface.Update(ref gameInput);           // (3)
    GameInput.Update(ref gameInput, hardInput);    // (4)
    var gameReport = Game.Update(gameInput, time); // (5)
    UserInterface.Render(gameReport, time);        // (6)
    GameAudio.Update(gameReport);                  // (7)
}
```

(1) Receives hardware input and delta time for this frame.  
(2) Creates a struct that contains input for the game simulation to execute.  
(3) Calls the UserInterface Update which changes the game input when necessary. i.e. when the button to start the game is pressed.  
(4) Calls the GameInput update which transforms hardware input into game input.  
(5) Calls the Game update which changes the game simulation based on the input, generating a report containing change information.  
(6) Calls another UserInterface function that uses the game report to update data on screen or to start animations.  
(7) Calls the GameAudio update which plays sound effects based on the changes on the game simulation.

Because there's only one entry point for every frame, it is easy to change the way the frame is defined. For example, we can simulate any frame time we want or mock the input if we need to. We can also easily change which Unity frame function (Update/LateUpdate/FixedUpdate) we want the game to run.

### Fixed Update

Since the introduction of the new Input System, we can now read user input from the FixedUpdate. This enables us to write game logic in this event, gaining the following advantages:

- Game logic and physics force changes can be done in the same frame function.
- Avoids problems with very high or very low frame times.
- Frees resources for better rendering performance.
- Improves the game determinism.

While having the following disadvantages:

- Rendering two frames without changes in the game simulation between them is not ideal because their results would be identical and as such GPU resources are spent without necessity. This can be solved by implementing an interpolation on the visuals between game states.
- If game simulation frames start to take more than the fixed time step to finish then the game is going to start falling behind the real-time. There's no real solution to this besides hoping the frames get faster again or dropping them.

### Automated Tests

We can create tests that pass through every system by creating a similar frame function from the one in Main and mocking the hardware input instead of gathering them from the real devices.

Furthermore, any system can be easily tested individually by calling its update function with the corresponding input and then analyzing the returned report and/or checking the changes in its internal state. An example of this type of test is implemented in the GameTests class of this project.

Because all the game logic is not tied to the Update event from Unity, these tests can be run as EditMode Tests. This means that tests that would otherwise require a lot of real-time to finish are executed as fast as possible, making it feasible to execute them frequently as opposed to waiting for a CI process to be run, for example.

### Performance

This architecture allows for any type of design to be used inside a system class. For example, the Game class could be constructed using a data-oriented design or ECS, which can leverage better use of the CPU cache than the usual Unity component system.

Moreover, this project contains two new types of containers that help increasing the overall performance of the game:

- ReusableArray: an encapsulation of an array with an integer that indicates how many elements of the array are set. It behaves very similarly to a List but has fixed size which may help avoid unexpected heap allocations.

- ArrayLinkedList: doubly linked list implemented with an array. It has O(1) complexity for inserting, removal, and random access. If the element type is a struct, the data also has the advantage of being laid out contiguously in memory. The disadvantages are its fixed size and higher memory footprint compared to a regular List.

This project also uses the usual object pool techniques to avoid unnecessary instantiations on runtime. All the objects are created in editor time inside the scenes and are reused while executing. The ideal process is to add beforehand enough elements based on the quantity necessary for the current gameplay implementation.

### Physics

As previously mentioned, running the game simulation in the FixedUpdate brings the advantage of tying it to the physics simulation. But if we want even more control we can even opt to disable the automatic physics simulation from Unity and then run it ourselves at any part of the frame.

Receiving collision events, though, is a little bit of an obstacle. Unity only allows receiving collision events by the OnCollision/OnTrigger functions on MonoBehaviours, forcing us to work with some type of callback-based design.

The ideal approach that would benefit the most this architecture would be to receive the resulted collision events directly from the physics frame simulation call, something like the following:

```csharp
CollisionEvents[] collisionEvents = Physics.Simulate(time.DeltaSeconds);
```

Unfortunately, this is yet no possible. In the meantime, this problem was solved in this project by using a workaround where every collision event is stored into collections which are then read and cleared in the next frame.

### Rendering

Currently, the game simulation and rendering are mixed up in the Game class, noted by the access of Transforms directly. It was made this way to boost productivity, but those concepts can be separated if necessary. Doing so would facilitate adding rendering interpolation between game states and also allow testing the game simulation without the hurdle of loading a scene beforehand.

### Design Decisions

One of the most proclaimed benefits for the adoption of OOP or the Unity Component System is because of their capabilities of separating concepts into smaller parts that can be easily reused. This is usually done by instantiating news classes or attaching new components to objects. But, in my experience, the reality is that as the project starts to grow, this newly added class or component often needs to be known from other classes. This dependency, when not well documented or communicated, can cause delays in productivity.

One common solution to this problem is to create some type of design or engineering to replace this coupling, such as using Singletons or ScriptableObjects to act as mediators. But those come with their own problems.

However, in my opinion, the biggest problem with micro separations and over-engineering is that the project becomes more and more complex to understand on a macro level. This becomes obvious when you try asking for someone new to the project to figure out how it works by looking only at the code and a scene full of game objects. It's easy to spot the individual parts but the game as a whole becomes hard to grasp.

This project solves this by having all the code laid out in plain code, in one place, and in sequence. Taking a look at the Update function from the Main class, for example, we can easily understand all the operations that the game does every frame. All of this without losing the things that the engine provides such as rendering, particles, editor, physics, and build system.

One downside to this approach is that it becomes harder to understand asynchronous operations because we are dealing with things frame by frame. As such, I would advise using other types of architectures for parts that are not performance dependant, such as complex UIs, communication with the Backend, or integration with other services. For those, I would prefer a more functional approach using reactive extensions (commonly accomplished using the UniRx library).

Another downside is that there is little forced design from the code structure that could help guide developers to maintain the architecture. This means that, despite having more freedom, we have more responsibility to follow the informal principles when adding new code to the project. This can be remediated though by a higher rate of review towards code added from beginner developers.



