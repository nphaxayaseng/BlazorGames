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