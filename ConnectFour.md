# ConnectFour

## How To Make ConnectFour in Blazor

### Modeling the Game

Before we can start writing the code we must take a minute to model the game of ConnectFour and the kinds of classes we need. Let's think about what kinds of things we can use to represent a game of ConnectFour in process.

First and foremost we have the "board." In the real world it would be a vertical series of slots, 7 spaces wide and 6 tall, into which the pieces are inserted. Then we have the pieces themselves, red and yellow discs that slide into the board. This is a relatively simple game, so the amount of modeling we have to do is limited.

#### The First Few Classes and Enums

Game pieces in ConnectFour are either red or yellow, and a single space on the game board can also be blank. Therefore we can use an enumeration to represent the possible colors the board can show. Create a `PieceColor.cs` file in the `Enums` folder:

```csharp
namespace BlazorGames.Models.ConnectFour.Enums
{
    public enum PieceColor
    {
        Red,
        Yellow,
        Blank
    }
}
```

We now need a basic `GamePiece.cs` class to represent a game piece. The game piece really only has one property: its color.

```csharp
using BlazorGames.Models.ConnectFour.Enums;

namespace BlazorGames.Models.ConnectFour
{
    public class GamePiece
    {
        public PieceColor Color;

        public GamePiece()
        {
            Color = PieceColor.Blank;
        }

        public GamePiece(PieceColor color)
        {
            Color = color;
        }
    }
}
```

Of course, we also need to model the board itself. We're going to think of the board as being a multidimensional array, 7 wide by 6 tall, and filled with "blank" pieces at the beginning of the game; these blank pieces will be replaced by colored pieces as the game progresses.

`GameBoard.cs`:

```csharp
using BlazorGames.Models.ConnectFour.Enums;
using System.Collections.Generic;

namespace BlazorGames.Models.ConnectFour
{
    public class GameBoard
    {
        public GamePiece[,] Board { get; set; }

        public PieceColor CurrentTurn { get; set; } = PieceColor.Red;

        public WinningPlay WinningPlay { get; set; }


        public GameBoard()
        {
            Reset();
        }

        public void Reset()
        {
            Board = new GamePiece[7, 6];

            //Populate the Board with blank pieces
            for (int i = 0; i <= 6; i++)
            {
                for (int j = 0; j <= 5; j++)
                {
                    Board[i, j] = new GamePiece(PieceColor.Blank);
                }
            }

            CurrentTurn = PieceColor.Red;
            WinningPlay = null;
        }
    }
}
```

That's all the models we need! Now we can get started modeling the actual game.

### Creating a New Razor Component

At this point, you need to create a new `ConnectFour.razor` component in your `Pages` folder. The created page will look something like this:

```html
@page "/connectfour"
@using BlazorGames.Models.ConnectFour;
@using BlazorGames.Models.ConnectFour.Enums;
@inject IJSRuntime _jsRuntime;

@code {
    GameBoard board = new GameBoard();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _jsRuntime.InvokeVoidAsync("Prism.highlightAll");
    }
}

<PageTitle Title="ConnectFour"/>
```

Note the `@page` directive. This directive sets up the route to which this page will answer to; in that way it is similar to Attribute Routing in ASP.NET MVC. Our route says this page will respond to <rootsite>/connectfour.

We also need to include the namespaces for our created classes and enumeration; for this application, they are `BlazorGames.Models.ConnectFour` and `BlazorGames.Model.ConnectFour.Enums`, which are used in the `@using` statements above.

Finally, note the `@code` directive. This directive tells ASP.NET Core to interpret anything between the curly braces as C# code, which will subsequently be compiled to Web Assembly. We will be putting as little as possible into that directive; most of the code will be included in the `Models` namespace. We only need a `GameBoard` class declared in that directive.

### Making the Game Look Nice

This game isn't going to look like anything if we don't put together some nice CSS for it. Here's the CSS classes for `games.css`:

```css
.connectfour-board {
    background-color: blue;
    display: flex;
    flex-direction: row;
    max-width: 490px;
}

.connectfour-column {
    min-width: 60px;
    min-height: 60px;
    height: 100%;
}

.connectfour-gamepiece {
    border: solid 1px black;
    margin: 5px;
    min-height: 60px;
    min-width: 60px;
    border-radius: 50px;
}

.connectfour-red {
    background-color: red;
}

.connectfour-yellow {
    background-color: yellow;
}

.connectfour-blank {
    background-color: white;
}
```

We'll be using all of these classes in just a little bit.

### Outputting the Board to the Page

Now let's see how we can use Blazor to output our starting board to the page. If a piece is already at the location clicked, nothing should happen. Here's the code for that in `ConnectFour.razor`:

```html
<div class="connectfour-board">
    @for (int i = 0; i < 7; i++)
    {
        < div class="connectfour-column">
        @for (int j = 0; j < 6; j++)
        {
            int x = i;
            int y = j;
            if (color == PieceColor.Blank)
            {
                <div class="connectfour-gamepiece connectfour-blank" 
                        @onclick="@(() => board.PieceClicked(x,y))"></div>
            }
            else
            {
                <div class="connectfour-gamepiece connectfour-@color.ToString().ToLower()"
                        style="@(board.IsGamePieceAWinningPiece(i, j)? "opacity: 0.6" : "")"></div>
            }
        }
    </div>
    }
</div>
```

There's something a bit strange in this code sample that I want to point out: why do we create the variables x and y if there are already variables i and j that represent the same thing? The reason is because of the way Blazor compiles C# code: the variables that are the "loop" variables are only available within the loops that created them. We create x and y to get around this limitation.

### Clicking a Piece

The `@onclick` declaration for our game piece `<div>` calls a method named `PieceClicked()` in the `GameBoard` object. Before we can write this method, we must first think about how pieces are supposed to behave in ConnectFour.

When you insert a piece into a column slot, they will slide down to the lowest available slot due to gravity. Our code, therefore, needs to reflect that no matter where in a column we click, the piece must "fall" to the lowest available slot. This is why we needed the "blank" color in the `PieceColor` enum.

Let's write that method now, with annotations:

```csharp
public void PieceClicked(int x, int y)
{
    //If a winning play has already been made, don't do anything.
    if (WinningPlay != null) { return; }

    GamePiece clickedSpace = Board[x, y];

    //The piece must "fall" to the lowest unoccupied space in the clicked column
    if (clickedSpace.Color == PieceColor.Blank)
    {
        while (y < 5)
        {
            GamePiece nextSpace = Board[x, y + 1];

            y = y + 1;
            if (nextSpace.Color == PieceColor.Blank)
            {
                clickedSpace = nextSpace;
            }
        }
        clickedSpace.Color = CurrentTurn;
    }

    //After every move, check to see if that move was a winning move.
    WinningPlay = GetWinner();
    if (WinningPlay == null)
    {
        SwitchTurns();
    }
}

public void SwitchTurns()
{
            CurrentTurn = CurrentTurn == PieceColor.Red ? PieceColor.Yellow : PieceColor.Red;
}
```

The method above will allow the player to click an unoccupied space and have their color piece "fall" to the lowest unoccupied space.

There's still something we have to do, however; we need to determine when a winning play is made, and point out which pieces were a part of that win. To do that, let's implement the `GetWinner()` method.

### Finding the Game Winner

In order to find the game winner, we are going to brute-force check each piece for a four-in-a-row. Once one is found, we can stop looking. A new class is needed: `WinningPlay`.

```csharp
using BlazorGames.Models.ConnectFour.Enums;
using System.Collections.Generic;

namespace BlazorGames.Models.ConnectFour
{
    public class WinningPlay
    {
        public List<string> WinningMoves { get; set; }
        public EvaluationDirection WinningDirection { get; set; }
        public PieceColor WinningColor { get; set; }
    }
}
```

We also need an enumeration `EvaluationDirection`, which determines in which direction we can evaluate the board for winning plays.

```csharp
namespace BlazorGames.Models.ConnectFour.Enums
{
    public enum EvaluationDirection
    {
        Up,
        UpRight,
        Right,
        DownRight
    }
}
```

We can now write the method to find the winning play. Note that this method will be called after every play. In `ConnectFour.razor`, just below the page title, add

```html
@if (board.WinningPlay == null)
{
    <h3>@board.CurrentTurn's Turn!</h3>

}
else
{
    <h3>@board.WinningPlay.WinningColor Wins! <button class="btn btn-success" @onclick="@(() => board.Reset())">Reset</button></h3>
}
```



```csharp
private WinningPlay GetWinner()
{
    WinningPlay winningPlay = null;

    for (int i = 0; i < 7; i++)
    {
        for (int j = 0; j < 6; j++)
        {
            winningPlay = EvaluatePieceForWinner(i, j, EvaluationDirection.Up);
            if (winningPlay != null) { return winningPlay; }

            winningPlay = EvaluatePieceForWinner(i, j, EvaluationDirection.UpRight);
            if (winningPlay != null) { return winningPlay; }

            winningPlay = EvaluatePieceForWinner(i, j, EvaluationDirection.Right);
            if (winningPlay != null) { return winningPlay; }

            winningPlay = EvaluatePieceForWinner(i, j, EvaluationDirection.DownRight);
            if (winningPlay != null) { return winningPlay; }
        }
    }
    return winningPlay;
}

private WinningPlay EvaluatePieceForWinner(int i, int j, EvaluationDirection dir)
{
    GamePiece currentPiece = Board[i, j];
    if (currentPiece.Color == PieceColor.Blank)
    {
        return null;
    }

    int inARow = 1;
    int iNext = i;
    int jNext = j;

    var winningMoves = new List<string>();

    while (inARow < 4)
    {
        switch (dir)
        {
            case EvaluationDirection.Up:
            jNext = jNext - 1;
            break;

            case EvaluationDirection.UpRight:
            iNext = iNext + 1;
            jNext = jNext - 1;
            break;

            case EvaluationDirection.Right:
            iNext = iNext + 1;
            break;

            case EvaluationDirection.DownRight:
            iNext = iNext + 1;
            jNext = jNext + 1;
            break;
        }

        if (iNext < 0 || iNext >= 7 || jNext < 0 || jNext >= 6) { break; }

        if (Board[iNext, jNext].Color == currentPiece.Color)
        {
            winningMoves.Add($"{iNext},{jNext}");
            inARow++;
        }
        else
        {
            return null;
        }
    }

    if (inARow >= 4)
    {
        winningMoves.Add($"{i},{j}");

        return new WinningPlay()
        {
            WinningMoves = winningMoves,
            WinningColor = currentPiece.Color,
            WinningDirection = dir,
        };
    }

    return null;
}
```

### We're Done!

That's it! We now have a working game of ConnectFour.