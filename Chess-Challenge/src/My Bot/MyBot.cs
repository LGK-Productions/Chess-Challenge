using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    static readonly int[] pieceValues = new int[7];

    static MyBot()
    {
        pieceValues[(int)PieceType.Pawn] = 10;
        pieceValues[(int)PieceType.Knight] = 30;
        pieceValues[(int)PieceType.Bishop] = 35;
        pieceValues[(int)PieceType.Rook] = 50;
        pieceValues[(int)PieceType.Queen] = 90;
        pieceValues[(int)PieceType.King] = 90;
    }

    const int targetDepth = 4;
    const int iterativeLayers = 2;
    private int branchesSearched;
    public Move Think(Board board, Timer timer)
    {
        branchesSearched = 0;

        //find the max evaluation of all possible moves
        bool isWhite = board.IsWhiteToMove;
        int bestValue = isWhite ? int.MinValue : int.MaxValue;
        Move bestMove = new();
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            int eval = MinMax(board, timer, targetDepth + iterativeLayers - 1, alpha, beta);
            board.UndoMove(move);

            if (isWhite ? eval > bestValue : eval < bestValue)
            {
                bestValue = eval;
                bestMove = move;
            }

            if (isWhite)
                alpha = Math.Max(alpha, bestValue);
            else
                beta = Math.Min(beta, bestValue);
            if (beta <= alpha)
                break;
        }

        Console.WriteLine("Searched " + branchesSearched + " branches. Best Move is " + bestMove.MovePieceType + " to " + bestMove.TargetSquare + ". Eval: " + bestValue);
        Console.WriteLine("Time per 1.000.000 branches: " + timer.MillisecondsElapsedThisTurn / (branchesSearched / 1_000_000f));
        return bestMove;
    }

    private int MinMax(Board board, Timer timer, int depth, int alpha, int beta)
    {
        if (board.IsInCheckmate())
            return board.IsWhiteToMove ? int.MinValue + (targetDepth + iterativeLayers - depth) : int.MaxValue - (targetDepth + iterativeLayers - depth);
        if (board.IsDraw())
            return 0;
        if (depth <= 0)
            return Eval(board);

        bool isWhite = board.IsWhiteToMove;
        int bestValue = isWhite ? int.MinValue : int.MaxValue;

        Move[] moves = board.GetLegalMoves();

        // Branch sorting: This makes it more likely for branches to be pruned
        List<int> skippedMoves = new(moves.Length);
        for (int i = 0; i < moves.Length; i++)
        {
            ulong bitboard = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;
            board.MakeMove(moves[i]);
            ulong newBitboard = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;

            if (newBitboard >= bitboard) // No piece was taken, ignore for now
            {
                board.UndoMove(moves[i]);
                skippedMoves.Add(i);

                continue;
            }

            // Iterative deepening: Go further if a piece was taken
            int newDepth = depth - 1;
            bestValue = isWhite ? Math.Max(bestValue, MinMax(board, timer, newDepth, alpha, beta))
                : Math.Min(bestValue, MinMax(board, timer, newDepth, alpha, beta));
            board.UndoMove(moves[i]);

            if (isWhite)
                alpha = Math.Max(alpha, bestValue);
            else
                beta = Math.Min(beta, bestValue);
            if (beta <= alpha)
                break;
        }

        foreach (var move in skippedMoves) //check remaining moves with no pieces taken
        {
            int newDepth = depth <= iterativeLayers ? 0 : depth - 1;
            board.MakeMove(moves[move]);
            bestValue = isWhite ? Math.Max(bestValue, MinMax(board, timer, newDepth, alpha, beta))
                : Math.Min(bestValue, MinMax(board, timer, newDepth, alpha, beta));
            board.UndoMove(moves[move]);

            if (isWhite)
                alpha = Math.Max(alpha, bestValue);
            else
                beta = Math.Min(beta, bestValue);
            if (beta <= alpha)
                break;
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

        int eval = 0;
        for (PieceType pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
        {
            eval += (board.GetPieceList(pieceType, true).Count 
                     - board.GetPieceList(pieceType, false).Count) 
                    * pieceValues[(int)pieceType];
        }

        return eval;
    }
}
