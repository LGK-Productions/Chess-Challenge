using System;
using System.Collections;
using System.Collections.Generic;
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
    private int minDepth = 3;
    private int maxDepth = 5;
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
            int eval = MinMax(board, timer, maxDepth, alpha, beta);
            board.UndoMove(move);
            
            if (isWhite ? eval > bestValue : eval < bestValue)
            {
                bestValue = eval;
                bestMove = move;
            }
            
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
        Console.WriteLine("Time per 1000000 branches: " + (float)timer.MillisecondsElapsedThisTurn / ((float)branchesSearched / 1000000f));
        return bestMove;
    }

    private int MinMax(Board board, Timer timer, int depth, int alpha, int beta)
    {
        if (depth <= 0)
            return Eval(board);
        
        bool isWhite = board.IsWhiteToMove;
        int bestValue = isWhite ? int.MinValue : int.MaxValue;

        Move[] moves = board.GetLegalMoves();
        
        //branch sorting. This makes it more Likely for branches to be pruned
        List<int> skippedMoves = new List<int>(moves.Length);
        for (int i = 0; i < moves.Length; i++)
        {
            ulong bitboard = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;
            board.MakeMove(moves[i]);
            ulong newBitboard = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;
            
            if (newBitboard < bitboard) //a piece was taken, so prioritize this move
            {
                //Iterative deepening: go further if a piece was taken
                int newDepth = depth - 1;
                bestValue = isWhite ? Math.Max(bestValue, MinMax(board, timer, newDepth, alpha, beta)) 
                    : Math.Min(bestValue, MinMax(board, timer, newDepth, alpha, beta));
                board.UndoMove(moves[i]);
                
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
            else
            {
                board.UndoMove(moves[i]);
                skippedMoves.Add(i);
            }
            
        }

        foreach (var move in skippedMoves) //check remaining moves with no pieces taken
        {
            int newDepth = depth <= maxDepth - minDepth ? 0 : depth - 1;
            board.MakeMove(moves[move]);
            bestValue = isWhite ? Math.Max(bestValue, MinMax(board, timer, newDepth, alpha, beta)) 
                : Math.Min(bestValue, MinMax(board, timer, newDepth, alpha, beta));
            board.UndoMove(moves[move]);   
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
        branchesSearched++;
        if (board.IsDraw())
            return 0;
        if (board.IsInCheckmate())
            return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        
        int eval = 0;
        foreach (PieceType pieceType in pieceValues.Keys)
        {
            eval += (board.GetPieceList(pieceType, true).Count - board.GetPieceList(pieceType, false).Count) * (int)pieceValues[pieceType];
        }
        return eval;
    }
}
