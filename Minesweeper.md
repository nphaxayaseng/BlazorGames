# Minesweeper

## How To Make Minesweeper in Blazor WebAssembly

Minesweeper is considerably more complex than ConnectFour.

The tutorial assumes you have ASP.NET Core 3.1 or greater.

### Modeling the Game

In order to model a game of Minesweeper, we must break it down into components that we can use. Let's take a look at a game of Minesweeper in progress:

![img](https://confident-ardinghelli-29e021.netlify.app/images/minesweeper/minesweeper-example.png)

Our modeling starts with the individual little "panels" on a game of Minesweeper. Each panel can be either unrevealed (the starting state) or revealed, and once revealed, each panel has either a mine, a number that represents the count of adjacent mines, or a blank space. Further, an unrevealed panel can be "flagged" so that the user cannot accidentally click on it; this is done to mark where the mines likely are.

Let's create a C# class called `Panel.cs` that has all of these attributes:

```csharp
namespace BlazorGames.Models.Minesweeper
{
    public class Panel
    {
        public int ID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsMine { get; set; }
        public int AdjacentMines { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsFlagged { get; set; }

        public Panel(int id, int x, int y)
        {
            ID = id;
            X = x;
            Y = y;
        }
    }
}
```

Each panel has two possible actions that can be performed against it: they can be revealed (when clicked), or flagged (when right-clicked).

```csharp
public class Panel
{
    //... Properties

    public void Flag()
    {
        if(!IsRevealed)
        {
            IsFlagged = !IsFlagged;
        }
    }

    public void Reveal()
    {
        IsRevealed = true;
        IsFlagged = false; //Revealed panels cannot be flagged
    }
}
```

As far as the panels are concerned, this is the extent of their functionality. The real work comes next, as we begin to design the game board itself.

#### The Game Board

A game of Minesweeper takes place on a board of X width and Y height, where those numbers are defined by the user. Each board has Z mines hidden in its panels. For every panel that is not a mine, a number is placed showing the count of mines adjacent to that panel. We therefore need our `GameBoard` class to keep track of its own dimensions, mine count, and collection of panels.

```csharp
using BlazorGames.Models.Minesweeper.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace BlazorGames.Models.Minesweeper
{
    public class GameBoard
    {
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        public int MineCount { get; set; } = 40;
        public List<Panel> Panels { get; set; }
    }
}
```

We will be greatly expanding this class as we develop our game.

#### The Game Status

Because the appearance of the Minesweeper board is determined by the state of the game, we can use an enumeration to track said state.

```csharp
namespace BlazorGames.Models.Minesweeper.Enums
{
    public enum GameStatus
    {
        AwaitingFirstMove,
        InProgress,
        Failed,
        Completed
    }
}
```

We must also allow the board to track the game state:

```csharp
public class GameBoard
{
    //...Other properties

    public GameStatus Status { get; set; }
}
```

Now let's build out the functionality of this board, starting with this question: how do "begin" a game of Minesweeper?

#### Initializing the Game

Every game of Minesweeper begins in the same state, with all panels hidden. Let's create a method to initialize a game, including creating the collection of panels. Given the height, width, and mine count as parameters, here's what our initialization method would look like:

```csharp
public class GameBoard
{
    //...Properties

    public void Initialize(int width, int height, int mines)
    {
        Width = width;
        Height = height;
        MineCount = mines;
        Panels = new List<Panel>();

        int id = 1;
        for (int i = 1; i <= height; i++)
        {
            for (int j = 1; j <= width; j++)
            {
                Panels.Add(new Panel(id, j, i));
                id++;
            }
        }

        Status = GameStatus.AwaitingFirstMove;
    }
}
```

You might be wondering: why didn't we place the mines and calculate the adjacent numbers? That's because of a trick in most implementations of Minesweeper: they don't calculate where the mines are placed until after the user makes the first move. This is so that the user doesn't click on a mine on the first move, because that's no fun.

For later usage, let's also implement a Reset method, which will reset the board to a new game using the same width, height, and mine count:

```csharp
public class GameBoard
{
    //...Other Properties and Methods

    public void Reset()
    {
        Initialize(Width, Height, MineCount);
    }
}
```

Now we can work on the next major step of this implementation: the first move.

#### The First Move and Getting Neighbors

We now need a method which represents the user's first move, and therefore determines where the mines go and calculates the adjacent mines numbers. That algorithm looks something like this:

1. When the user makes the first move, take that panel and a certain number of neighbor panels, and mark them as unavailable for mines.
2. For every other panel, place the mines randomly.
3. For every panel which is not a mine (including the panels from Step 1), calculate the adjacent mines.
4. Mark the game as started.

The trickiest part of this algorithm is calculating the neighbors of a given panel. Remember that the neighbors of any given panel are the first panel above, below, to the left, to the right, and on each diagonal.

![img](https://confident-ardinghelli-29e021.netlify.app/images/minesweeper/minesweeper-neighbors.png)

For our implementation, we have an entire method to do this:

```csharp
public class GameBoard
{
    //...Other Properties and Methods

    public List<Panel> GetNeighbors(int x, int y)
    {
        var nearbyPanels = Panels.Where(panel => panel.X >= (x - 1)
        && panel.X <= (x + 1)
        && panel.Y >= (y - 1)
        && panel.Y >= (y + 1));

        var currentPanel = Panels.Where(panel => panel.X == x && panel.Y == y);

        return nearbyPanels.Except(currentPanel).ToList();
    }
}
```

Using our "first move" algorithm, the new `GetNeighbors()` method, and some fancy LINQ, we end up with this method to implement the first move:

```csharp
public class GameBoard
{
    //...Other Properties and Methods
    
    public void FirstMove(int x, int y)
    {
        Random rand = new Random();

        //For any board, take the user's first revealed panel 
        // and any neighbors of that panel, and mark them 
        // as unavailable for mine placement.
        var neighbors = GetNeighbors(x, y); //Get all neighbors
        
        //Add the clicked panel to the "unavailable for mines" group.
        neighbors.Add(Panels.First(z => z.X == x && z.Y == y));

        //Select all panels from set which are available for mine placement.
        //Order them randomly.
        var mineList = Panels.Except(neighbors)
                             .OrderBy(user => rand.Next());
                             
        //Select the first Z random panels.
        var mineSlots = mineList.Take(MineCount)
                                .ToList()
                                .Select(z => new { z.X, z.Y });

        //Place the mines in the randomly selected panels.
        foreach (var mineCoord in mineSlots)
        {
            Panels.Single(panel => panel.X == mineCoord.X 
                                   && panel.Y == mineCoord.Y)
                  .IsMine = true;
        }

        //For every panel which is not a mine, 
        // including the unavailable ones from earlier,
        // determine and save the adjacent mines.
        foreach (var openPanel in Panels.Where(panel => !panel.IsMine))
        {
            var nearbyPanels = GetNeighbors(openPanel.X, openPanel.Y);
            openPanel.AdjacentMines = nearbyPanels.Count(z => z.IsMine);
        }

        //Mark the game as started.
        Status = GameStatus.InProgress;
    }
}
```

There's one last thing we need to finish the first move implementation: a method that activates when a user makes a move, and if it is the first move, calls our `FirstMove()` method. Here's that method:

```csharp
public class GameBoard
{
    //...Other Properties and Methods
    
    public void MakeMove(int x, int y)
    {
        if (Status == GameStatus.AwaitingFirstMove)
        {
            FirstMove(x, y);
        }
        RevealPanel(x, y);
    }
}
```

All right! First move implementation is complete, and we can now move on to what happens on every other panel.

#### Revealing Panels

For every move except the first one, clicking on a panel reveals that panel. A more specific algorithm for moves after the first one might be this:

1. When the user left-clicks on a panel, reveal that panel.
2. If that panel is a mine, show all mines and end the game.
3. If that panel is a zero, show that panel and all neighbors. If any neighbors are also zero, show all their neighbors, and continue until all adjacent zeroes and their neighbors are revealed. (I termed this a "cascade reveal").
4. If that panel is NOT a mine, check to see if any remaining unrevealed panels are not mines. If there are any, the game continues.
5. If all remaining unrevealed panels are mines, the game is complete. This is the "win" condition.

Using that algorithm, we end up with this method:

```csharp
public class GameBoard
{
    //...Other Properties and Methods
    
    public void RevealPanel(int x, int y)
    {
        //Step 1: Find and reveal the clicked panel
        var selectedPanel = Panels.First(panel => panel.X == x 
                                                  && panel.Y == y);
        selectedPanel.Reveal();

        //Step 2: If the panel is a mine, show all mines. Game over!
        if (selectedPanel.IsMine)
        {
            Status = GameStatus.Failed;
            RevealAllMines();
            return;
        }

        //Step 3: If the panel is a zero, cascade reveal neighbors.
        if (selectedPanel.AdjacentMines == 0)
        {
            RevealZeros(x, y);
        }

        //Step 4: If this move caused the game to be complete, mark it as such
        CompletionCheck();
    }
}
```

We now have several methods to create to finish this part of our implementation.

#### Revealing All Mines

The simplest of the new methods we need is this one: showing all mines on the board.

```csharp
public class GameBoard
{
    //...Other Methods and Properties

    private void RevealAllMines()
    {
        Panels.Where(x => x.IsMine)
        	  .ToList()
              .ForEach(x => x.IsRevealed = true);
    }
}
```

#### Cascade Reveal Neighbors

When a zero-value panel is revealed, the game needs to then reveal all of that panel's neighbors, and if any of the neighbors are zero, reveal the neighbors of that panel as well. This calls for a recursive method:

```csharp
public class GameBoard
{
    //...Other Methods and Properties
    
    public void RevealZeros(int x, int y)
    {
        //Get all neighbor panels
        var neighborPanels = GetNeighbors(x, y)
                               .Where(panel => !panel.IsRevealed);
                               
        foreach (var neighbor in neighborPanels)
        {
            //For each neighbor panel, reveal that panel.
            neighbor.IsRevealed = true;
            
            //If the neighbor is also a 0, reveal all of its neighbors too.
            if (neighbor.AdjacentMines == 0)
            {
                RevealZeros(neighbor.X, neighbor.Y);
            }
        }
    }
}
```

#### Is The Game Complete?

A game of Minesweeper is complete when all remaining unrevealed panels are mines. We can use this method to check for that:

```csharp
public class GameBoard
{
    //...Other Properties and Methods
    
    private void CompletionCheck()
    {
        var hiddenPanels = Panels.Where(x => !x.IsRevealed)
                                 .Select(x => x.ID);
                                 
        var minePanels = Panels.Where(x => x.IsMine)
                               .Select(x => x.ID);
                               
        if (!hiddenPanels.Except(minePanels).Any())
        {
            Status = GameStatus.Completed;
            Stopwatch.Stop();
        }
    }
}
```

With this functionality in place, there's only one thing left to do: flagging.

#### Flagging a Panel

"Flagging" a panel means marking it as the location of a mine. Doing this means that left-clicking that panel will do nothing.

![img](https://confident-ardinghelli-29e021.netlify.app/images/minesweeper/minesweeper-flags.png)

In our game, we will use right-click to flag a panel. Recall that our `Panel` class already has a `Flag()` method. Now we just need to call that method from the `GameBoard` class:

```csharp
public class GameBoard
{
    //...Other Properties and Methods
    
    public void FlagPanel(int x, int y)
    {
        if (MinesRemaining > 0)
        {
            var panel = Panels.Where(z => z.X == x && z.Y == y).First();

            panel.Flag();
        }
    }
}
```

There's only a little bit of the C# implementation left to do.

#### Implementing a Timer

Take a look at the top part of the Minesweeper game we saw earlier:

![img](https://confident-ardinghelli-29e021.netlify.app/images/minesweeper/minesweeper-example.png)

Other than the smiley face's piercing stare, what do you notice? The counters!

The left counter is for mines; it goes down as the number of flagged panels increases. The right counter is for the time; it ticks up every second. We need to implement a timer for our Minesweeper game.

For this, we will use the C# [Stopwatch class](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=netcore-3.1). We will use the following algorithm:

1. Whenever the game is created or reset, we reset the timer.
2. When the player makes their first move, start the timer.
3. When the game is either complete or failed, stop the timer.

The changes we need to make to our `GameBoard` class look like this:

```csharp
public class GameBoard
{
    //...Other Properties
    public Stopwatch Stopwatch { get; set; }
    
    //...Other Methods
    
    public void Reset()
    {
        Initialize(Width, Height, MineCount);
        Stopwatch = new Stopwatch();
    }
    
    public void FirstMove()
    {
        //...Implementation
        Stopwatch.Start();
    }
    
    private void CompletionCheck()
    {
        var hiddenPanels = Panels.Where(x => !x.IsRevealed)
                                 .Select(x => x.ID);
                                 
        var minePanels = Panels.Where(x => x.IsMine)
                               .Select(x => x.ID);
                               
        if (!hiddenPanels.Except(minePanels).Any())
        {
            Status = GameStatus.Completed;
            Stopwatch.Stop(); //New line
        }
    }
}
```

Ta-da! With the timer implemented, all of our C# implementation for this Minesweeper game is complete! But we're not done yet: we still need to write up the Blazor component and wire everything up.

### Creating the Blazor Component

In Blazor, we do not have "pages"; rather we use Components (files that have the .razor suffix). To be fair, they look almost exactly like [Razor Pages](https://exceptionnotfound.net/tag/razorpages/).

Here's a starting Blazor Component (`Minesweeper.razor` file) we can use:

```csharp
@page "/minesweeper"
@using BlazorGames.Models.Minesweeper
@using BlazorGames.Models.Minesweeper.Enums

@code {
    GameBoard board = new GameBoard();
}

<PageTitle Title="Minesweeper" />
```

As a reminder, the `@page` directive sets the URI for the current component.

Let's take a look at a Minesweeper board again:

![img](https://confident-ardinghelli-29e021.netlify.app/images/minesweeper/minesweeper-almost-done.png)

See the border running around the game area and the header? We need to have a way to create that border. Since it is essentially an additional space on any given side of the board, we need extra space in our calculations. Keep that in mind.

For now, let's concentrate on the area of the board where the numbers and mines are. For the size of the board X by Y, we need X + 2 columns of Y + 2 height, where the extremes (uppermost, lowermost, leftmost, rightmost) rows and columns are the border. Therefore our markup might look like this:

```html
<div class="minesweeper-board">
    @{
        var maxWidth = board.Width + 1;
        var maxHeight = board.Height + 1;
    }

    @for (int i = 0; i <= maxWidth; i++)
    {
        @for (int j = 0; j <= maxHeight; j++)
        {
            int x = i;
            int y = j;

            if (x == 0 && y == 0) //Upper-left corner
            {
                <div class="minesweeper-border-jointleft"></div>
            }
            else if (x == 0 && y == maxHeight) //Upper-right corner
            {
                <div class="minesweeper-border-jointright"></div>
            }
            else if (x == maxWidth && y == 0) //Lower-left corner
            {
                <div class="minesweeper-border-bottomleft"></div>
            }
            else if (x == maxWidth && y == maxHeight) //Lower-right corner
            {
                <div class="minesweeper-border-bottomright"></div>
            }
            else if (y == 0 || y == maxHeight) //Leftmost column
            {
                <div class="minesweeper-border-vertical"></div>
            }
            else if (x == 0 || x == maxWidth) //Rightmost column
            {
                <div class="minesweeper-border-horizontal"></div>
            }
            else if (y > 0 && y < maxHeight)
            {
                <!-- Output panels -->
            }
        }
    }
</div>
```

We now need to consider how to output the panels themselves.

#### Displaying the Panels

In our implementation a Panel may, at any time, be in one of five potential states:

- Revealed Mine
- Revealed Number
- Revealed Blank
- Flagged
- Unrevealed

We need special CSS classes for each state, and so the finished markup for the page will need to include the following:

```csharp
<div class="minesweeper-board">
@{
    var maxWidth = board.Width + 1;
    var maxHeight = board.Height + 1;
}

@for (int i = 0; i <= maxWidth; i++)
{
    @for (int j = 0; j <= maxHeight; j++)
    {
        int x = i;
        int y = j;

        if (x == 0 && y == 0) //Upper-left corner
        {
            <div class="minesweeper-border-jointleft"></div>
        }

        //...All the other IF clauses

        else if (y > 0 && y < maxHeight)
        {
            var currentPanel = board.Panels.First(m => m.X == x
                                                          && m.Y == y);
            if (currentPanel.IsRevealed)
            {
                if (currentPanel.IsMine) //Mine
                {
                    <div class="minesweeper-gamepiece minesweeper-mine"></div>
                }
                else if (currentPanel.AdjacentMines == 0) //Blank
                {
                    <div class="minesweeper-gamepiece minesweeper-0"></div>
                }
                else //Number
                {
                    <div class="minesweeper-gamepiece minesweeper-@currentPanel.AdjacentMines">@currentPanel.AdjacentMines</div>
                }
            }
            else if (currentPanel.IsFlagged)
            {
                <div class="minesweeper-gamepiece minesweeper-flagged"></div>
            }
            else //Unrevealed
            {
                <div class="minesweeper-gamepiece minesweeper-unrevealed"></div>
            }
        }
    }
}
</div>
```

There's an additional wrinkle to consider: in some of these states, namely the Flagged and Unrevealed ones, the user can interact with the panel by left clicking (to reveal) or right-clicking (to flag). We need to implement that functionality.

#### Left-Click to Reveal

You may recall that we implemented a `MakeMove()` method in the C# implementation. The reason was so that it could be called here. This is the markup for the unrevealed panel, now with an @onclick event:

```html
<div class="minesweeper-gamepiece minesweeper-unrevealed"
     @onclick="@(() => board.MakeMove(x, y))"
     @oncontextmenu="@(() => board.FlagPanel(x, y))"
     @oncontextmenu:preventDefault>
</div>
```

There's a lot to dissect here:

- The `@onclick` directive is a Blazor directive that binds a C# method to an onclick event. In this case, we are calling `MakeMove()` and passing the coordinates of the clicked panel.
- The `@oncontextmenu` directive allows us to specify what should happen when the context menu (AKA the right-click menu) should be displayed. Since we want to flag panels on right-click, we say that here.
- The `@oncontextmenu:preventDefault` is a specialized instruction to Blazor that prevents the context menu from displaying on right-clicks.

Let's also see the markup for the flagged panel:

```html
<div class="minesweeper-gamepiece minesweeper-flagged"
     @oncontextmenu="@(() => board.FlagPanel(x, y))"
     @oncontextmenu:preventDefault>
</div>
```

Okay! We have our playing area ready. Now let's build the header where the mine counter, timer, and face are placed.

#### The Header

 [Back to Top](https://confident-ardinghelli-29e021.netlify.app/#howtomake)

Here's the full markup for the header area:

```html
<div class="minesweeper-board"
     @oncontextmenu:preventDefault
     onmousedown="@(board.Status != GameStatus.Completed ? "faceOooh(event);" : "")" <!-- Explanation below -->
     onmouseup="faceSmile();">
<div class="minesweeper-border-topleft"></div>
@for (int i = 1; i < maxWidth; i++)
{
    <div class="minesweeper-border-horizontal"></div>
}
<div class="minesweeper-border-topright"></div>
<div class="minesweeper-border-vertical-long"></div>
<div class="minesweeper-time-@GetPlace(board.MinesRemaining, 100)"
     id="mines_hundreds"></div>
<div class="minesweeper-time-@GetPlace(board.MinesRemaining, 10)"
     id="mines_tens"></div>
<div class="minesweeper-time-@GetPlace(board.MinesRemaining, 1)"
     id="mines_ones"></div>
@if (board.Status == GameStatus.Failed)
{
    <div class="minesweeper-face-dead"
         id="face"
         style="margin-left:70px; margin-right:70px;"
         @onclick="@(() => board.Reset())"></div>
}
else if (board.Status == GameStatus.Completed)
{
    <div class="minesweeper-face-win"
         id="face"
         style="margin-left:70px; margin-right:70px;"
         @onclick="@(() => board.Reset())"></div>
}
else
{
    <div class="minesweeper-face-smile"
         id="face"
         style="margin-left:70px; margin-right:70px;"
         @onclick="@(() => board.Reset())"></div>
}
<div class="minesweeper-time-@GetPlace(board.Stopwatch.Elapsed.Seconds,100)"
     id="seconds_hundreds"></div>
<div class="minesweeper-time-@GetPlace(board.Stopwatch.Elapsed.Seconds,10)"
     id="seconds_tens"></div>
<div class="minesweeper-time-@GetPlace(board.Stopwatch.Elapsed.Seconds,1)"
     id="seconds_ones"></div>
<div class="minesweeper-border-vertical-long"></div>
</div>
```

#### Make 'Em Say "Oooh"

There are certain things that Blazor, in its current form, is just not good at. For example, in the desktop version of Minesweeper, whenever the user left-clicks a panel the smiley face turns to an "oooh" face. Blazor cannot do this (or, more accurately, I could not figure out how to do this in Blazor). So, I turned to good old JavaScript to get the face to say "oooh!".

```javascript
function faceOooh(event) {
    if (event.button === 0) { //Left-click only
        document.getElementById("face").className = "minesweeper-face-oooh";
    }
}

function faceSmile() {
    var face = document.getElementById("face");
    
    if (face !== undefined)
        face.className = "minesweeper-face-smile";
}
```

#### What Time Is It Anyway?

The timer proved to be the most difficult part of this entire implementation, and exactly why that is true requires some explaining.

Blazor has a special method, ```StateHasChanged()```, which allows developers to notify Blazor that the state of its object has changed and it should re-render the display. But the timer, you might remember, was implemented on the backend as a C# Stopwatch class, and of course the C# code has no way to notify the Blazor frontend that something has changed (namely, that a second has gone by).

So, we fake it, and that requires a couple parts. First a JavaScript method to display a passed-in time:

```javascript
function setTime(hundreds, tens, ones) {
    var hundredsElement = document.getElementById("seconds_hundreds");
    var tensElement = document.getElementById("seconds_tens");
    var onesElement = document.getElementById("seconds_ones");

    if (hundredsElement !== null) {
        hundredsElement.className = "minesweeper-time-" + hundreds;
    }
    if (tensElement !== null) {
        tensElement.className = "minesweeper-time-" + tens;
    }
    if (onesElement !== null) {
        onesElement.className = "minesweeper-time-" + ones;
    }
}
```

On the Blazor component itself, we use the method `OnAfterRenderAsync`, which fires after the control is rendered to the browser:

```csharp
@inject IJSRuntime _jsRuntime
@inject NavigationManager _navManager

@code {
 public int GetPlace(int value, int place)
    {
        if (value == 0) return 0;
        return ((value % (place * 10)) - (value % place)) / place;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        while (board.Status == GameStatus.InProgress && _navManager.Uri.Contains("minesweeper"))
        {
            await Task.Delay(500);
            var elapsedTime = (int)board.Stopwatch.Elapsed.TotalSeconds;
            var hundreds = GetPlace(elapsedTime, 100);
            var tens = GetPlace(elapsedTime, 10);
            var ones = GetPlace(elapsedTime, 1);

            await _jsRuntime.InvokeAsync<string>("setTime", hundreds, tens, ones);
        }

        await _jsRuntime.InvokeVoidAsync("Prism.highlightAll");
    }
}
```

Every half-second it invokes a JS method to display the new time. To be able to do this we had to inject two new dependencies to the component:

- `IJSRuntime`, which allows Blazor code to [invoke JavaScript functions](https://docs.microsoft.com/en-us/aspnet/core/blazor/call-javascript-from-dotnet?view=aspnetcore-3.1) AND
- `NavigationManager`, which allows [access to URIs used by a Blazor app](https://docs.microsoft.com/en-us/aspnet/core/blazor/routing?view=aspnetcore-3.1).

Obviously this is not a perfect solution, but it works.

### We're Done!

That was a lot of work, but we now have a Minesweeper game built in Blazor WebAssembly! 