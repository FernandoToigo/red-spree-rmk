# Red Spree Remake

The purpose of this project is to serve as an example of a different architecture for a Unity project which focuses on simplicity, productivity and performance. It uses predominantly structs and static classes while avoiding the default component architecture from Unity and OOP (reasons for those are explained at the end of this document[^1]).

#How does it work

### The Main Component

All game logic derives from the Update of a single MonoBehaviour called Main. This component is alive throughout the whole lifetime of the game and is responsible for calling the Update function from all the other classes (which are plain old static classes). This means that we can control the order of execution of the different systems without needing to worry about the script execution order from Unity.

Also, usage of callbacks is avoided and replaced by polling methods when possible. This makes the code easier to understand as a whole because there are never surprises on the call stack of function calls.

The Main class also manages the transition of states of the game such as splash, loading, main menu, changing of levels, and so forth. This component is contained in its own scene which is never unloaded and the transition of states is done by loading other scenes additively. As such, the Update function is always running and there is no need of using the DontDestroyOnLoad feature.

### The System Classes

Despite avoiding OOP we still want to maintain a certain level of abstraction. This is done by separating the different systems into specific classes which for the most part can work independently. In general, each of these classes contain the public functions Initialize and Update. More public functions can be added but that should be an exception in order to avoid the biggest cognitive challenge when trying to understand static classes: that there are usually too many external functions changing its state at random times.

These classes usually contains a public State field. The idea is that only the class itself can change its state based on the Initialize and Update functions and therefore external accesses of that State should be done exclusively for reading (though this is not enforced in order to avoid unnecessary copies, additional code and to boost productivity).

An update function usually has the following signature:

```csharp
public static State State;

public static Report Update(Input input)
{
   // Changes state based on input and return a report containing what changed.
}
```
Where Input is a struct containing what needs to be executed in this system and the returned Report is another struct containing what has changed. This report struct is then spread throughout the other systems, replacing the functionality which would used be accomplished by invoking events or using the observable pattern.

[^1]: Test.