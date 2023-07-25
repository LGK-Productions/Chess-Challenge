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
    private int branchesSearched;
    public Move Think(Board board, Timer timer)
    {
        branchesSearched = 0;
        
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
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        foreach (Move move in board.GetLegalMoves())
        {
            lastMoveFinishedTime = timer.MillisecondsElapsedThisTurn;
            board.MakeMove(move);
            int eval = MinMax(board, timer, 6, alpha, beta);

            if ((isWhite ? eval > bestValue : eval < bestValue))
            {
                bestValue = eval;
                bestMove = move;
            }
            board.UndoMove(move);

            if (isWhite)
            {
                alpha = Math.Max(alpha, bestValue);
                if (beta <= alpha)
                    break;
            } else 
            {
                beta = Math.Min(beta, bestValue);
                if (beta <= alpha)
                    break;
            }
        }

        Console.WriteLine("Searched " + branchesSearched + " branches. Best Move is " + bestMove.MovePieceType + " to " + bestMove.TargetSquare + ". Eval: " + bestValue);
        return bestMove;
    }

    private int MinMax(Board board, Timer timer, int depth, int alpha, int beta)
    {
        if (depth <= 0)
            return Eval(board);
        
        bool isWhite = board.IsWhiteToMove;
        int bestValue = isWhite ? int.MinValue : int.MaxValue;

        foreach (var move in board.GetLegalMoves())
        {
            int nextDepth = depth - Math.Max(1, (int)((timer.MillisecondsElapsedThisTurn - lastMoveFinishedTime) / msPerTurn * depth));
                
            board.MakeMove(move);
            if (isWhite)
                bestValue = isWhite ? Math.Max(bestValue, MinMax(board, timer, nextDepth, alpha, beta)) : Math.Min(bestValue, MinMax(board, timer, nextDepth, alpha, beta));
            board.UndoMove(move);

            if (isWhite)
            {
                alpha = Math.Max(alpha, bestValue);
                if (beta <= alpha)
                    break; 
            } else
            {
                beta = Math.Min(beta, bestValue);
                if (beta <= alpha)
                    break;
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

        branchesSearched++;
        return eval;
    }
}
