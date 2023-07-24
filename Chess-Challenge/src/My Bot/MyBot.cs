using System;
using System.Collections;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    private Hashtable pieceValues = new Hashtable()
    {
        { PieceType.Pawn, 10 },
        { PieceType.Bishop, 35 },
        { PieceType.Knight, 30 },
        { PieceType.Rook, 50 },
        { PieceType.Queen, 90 },
        { PieceType.King, 900 }
    };

    private int msPerTurn = 100;
    private float lastMoveFinishedTime;
    public Move Think(Board board, Timer timer)
    {
        //adjust target time for move calculation if necessary
        if (timer.MillisecondsRemaining < msPerTurn * 300)
        {
            msPerTurn = (int)(msPerTurn * 0.6f);
            Console.WriteLine("Adjusted ms per turn to " + msPerTurn);
        }
        
        //find the max evaluation of all possible moves
        bool isWhite = board.IsWhiteToMove;
        int bestValue = isWhite ? int.MinValue : int.MaxValue;
        Move bestMove = new Move();
        foreach (Move move in board.GetLegalMoves())
        {
            lastMoveFinishedTime = timer.MillisecondsElapsedThisTurn;
            board.MakeMove(move);
            int eval = MinMax(board, timer, 6, int.MinValue, int.MaxValue);
            
            if (isWhite ? eval > bestValue : eval < bestValue)
            {
                bestValue = eval;
                bestMove = move;
            }
            board.UndoMove(move);
        }

        return bestMove;
    }

    public int MinMax(Board board, Timer timer, int depth, int alpha, int beta)
    {
        if (depth <= 0)
            return Eval(board);
        
        bool isWhite = board.IsWhiteToMove;
        int bestValue = isWhite ? Int32.MinValue : Int32.MaxValue;
        foreach (var move in board.GetLegalMoves())
        {
            int nextDepth =
                depth - Math.Max(1, (int)((timer.MillisecondsElapsedThisTurn - lastMoveFinishedTime) / msPerTurn * depth)); 
            board.MakeMove(move);
            if (isWhite)
            {
                bestValue = Math.Max(bestValue, MinMax(board, timer, nextDepth, alpha, beta));
                board.UndoMove(move);
                if (bestValue > beta)
                    break;
                alpha = Math.Max(alpha, bestValue);
            }
            else
            {
                bestValue = Math.Min(bestValue, MinMax(board, timer, nextDepth, alpha, beta));
                board.UndoMove(move);
                if (bestValue < alpha)
                    break;
                beta = Math.Min(alpha, bestValue);
            }
            
        }
        return bestValue;
    }

    /// <summary>
    /// basic evaluation, just accounts for basic piece values
    /// </summary>
    /// <param name="board">the state of the board to evaluate</param>
    /// <returns>a static evaluation of the given position</returns>
    private int Eval(Board board)
    {
        int eval = 0;
        foreach (PieceType pieceType in pieceValues.Keys)
        {
            eval += (board.GetPieceList(pieceType, true).Count - board.GetPieceList(pieceType, false).Count) * (int)pieceValues[pieceType];
        }
        return eval;
    }
}
