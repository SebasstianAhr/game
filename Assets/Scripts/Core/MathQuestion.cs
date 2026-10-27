using System;
using System.Collections.Generic;

namespace OnePercent
{
    public enum QuestionDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>Una pregunta de matemáticas con 4 opciones y su premio (o castigo) en monedas.</summary>
    public class MathQuestion
    {
        public string Text;
        public int[] Options;
        public int CorrectIndex;
        public QuestionDifficulty Difficulty;
        public bool Mandatory;

        /// <summary>Monedas que se ganan si se acierta, y que se pierden si se falla o se acaba el tiempo.</summary>
        public int Reward;

        public int CorrectAnswer
        {
            get { return Options[CorrectIndex]; }
        }
    }

    /// <summary>
    /// Crea preguntas cuya dificultad sube con las rondas:
    /// fácil = sumas y restas; media = multiplicaciones y divisiones exactas; difícil = porcentajes y operaciones de dos pasos.
    /// </summary>
    public static class MathQuestionGenerator
    {
        public static QuestionDifficulty DifficultyFor(int roundNumber, GameConfig config)
        {
            if (roundNumber >= config.hardQuestionsFromRound)
            {
                return QuestionDifficulty.Hard;
            }

            if (roundNumber >= config.mediumQuestionsFromRound)
            {
                return QuestionDifficulty.Medium;
            }

            return QuestionDifficulty.Easy;
        }

        public static int RewardFor(QuestionDifficulty difficulty, bool mandatory, GameConfig config)
        {
            int reward;
            switch (difficulty)
            {
                case QuestionDifficulty.Hard: reward = config.hardQuestionReward; break;
                case QuestionDifficulty.Medium: reward = config.mediumQuestionReward; break;
                default: reward = config.easyQuestionReward; break;
            }

            return mandatory ? reward * config.mandatoryRewardMultiplier : reward;
        }

        /// <param name="roundNumber">Número de la ronda que se acaba de jugar (1, 2, 3...).</param>
        public static MathQuestion Generate(int roundNumber, bool mandatory, Random rng, GameConfig config)
        {
            QuestionDifficulty difficulty = DifficultyFor(roundNumber, config);
            string text;
            int answer;

            switch (difficulty)
            {
                case QuestionDifficulty.Easy:
                {
                    int a = rng.Next(8, 61);
                    int b = rng.Next(5, 41);
                    if (rng.Next(2) == 0)
                    {
                        text = a + " + " + b;
                        answer = a + b;
                    }
                    else
                    {
                        if (b > a)
                        {
                            int swap = a;
                            a = b;
                            b = swap;
                        }

                        if (a == b)
                        {
                            a += rng.Next(3, 15); // evita que el resultado sea 0
                        }

                        text = a + " - " + b;
                        answer = a - b;
                    }

                    break;
                }

                case QuestionDifficulty.Medium:
                {
                    int a = rng.Next(3, 13);
                    int b = rng.Next(3, 13);
                    if (rng.Next(2) == 0)
                    {
                        text = a + " × " + b;
                        answer = a * b;
                    }
                    else
                    {
                        // División exacta: se parte del resultado para que no haya decimales.
                        text = (a * b) + " ÷ " + b;
                        answer = a;
                    }

                    break;
                }

                default:
                {
                    if (rng.Next(2) == 0)
                    {
                        int[] percents = { 10, 15, 20, 25, 30, 40, 50, 75 };
                        int p = percents[rng.Next(percents.Length)];
                        int baseValue = 20 * rng.Next(1, 11); // 20, 40, ... 200: siempre da un resultado entero
                        text = p + " % de " + baseValue;
                        answer = p * baseValue / 100;
                    }
                    else
                    {
                        int a = rng.Next(3, 10);
                        int b = rng.Next(3, 10);
                        int c = rng.Next(5, 31);
                        text = a + " × " + b + " + " + c;
                        answer = a * b + c;
                    }

                    break;
                }
            }

            MathQuestion question = new MathQuestion();
            question.Text = text + " = ?";
            question.Difficulty = difficulty;
            question.Mandatory = mandatory;
            question.Reward = RewardFor(difficulty, mandatory, config);
            question.Options = BuildOptions(answer, rng, out question.CorrectIndex);
            return question;
        }

        /// <summary>La respuesta correcta y 3 distractores cercanos, todos distintos y positivos, en orden aleatorio.</summary>
        private static int[] BuildOptions(int answer, Random rng, out int correctIndex)
        {
            int[] offsets = { -10, -5, -2, -1, 1, 2, 5, 10, 3, -3 };
            List<int> candidates = new List<int>();
            for (int i = 0; i < offsets.Length; i++)
            {
                int value = answer + offsets[i];
                if (value > 0 && value != answer && !candidates.Contains(value))
                {
                    candidates.Add(value);
                }
            }

            List<int> options = new List<int> { answer };
            while (options.Count < 4 && candidates.Count > 0)
            {
                int pick = rng.Next(candidates.Count);
                options.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }

            // Respaldo (casi nunca necesario): números siguientes que no estén repetidos.
            int extra = answer + 11;
            while (options.Count < 4)
            {
                if (!options.Contains(extra))
                {
                    options.Add(extra);
                }

                extra++;
            }

            // Mezclar (Fisher-Yates).
            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = options[i];
                options[i] = options[j];
                options[j] = tmp;
            }

            correctIndex = options.IndexOf(answer);
            return options.ToArray();
        }
    }

    public enum QuestionTrigger
    {
        None,
        Optional,
        Mandatory
    }

    /// <summary>
    /// Decide cuándo aparece una pregunta: opcional cada cierto número aleatorio de rondas
    /// y obligatoria cada N rondas exactas (10, 20, 30...).
    /// </summary>
    public class QuestionScheduler
    {
        private readonly GameConfig config;
        private readonly Random rng;
        private int nextOptionalRound;

        public QuestionScheduler(GameConfig config, Random rng)
        {
            this.config = config;
            this.rng = rng;
            nextOptionalRound = NextGap();
        }

        /// <param name="roundsPlayed">Rondas jugadas en la partida, contando la que acaba de terminar.</param>
        public QuestionTrigger OnRoundPlayed(int roundsPlayed)
        {
            if (config.mandatoryQuestionEvery > 0 && roundsPlayed % config.mandatoryQuestionEvery == 0)
            {
                nextOptionalRound = roundsPlayed + NextGap();
                return QuestionTrigger.Mandatory;
            }

            if (roundsPlayed >= nextOptionalRound)
            {
                nextOptionalRound = roundsPlayed + NextGap();
                return QuestionTrigger.Optional;
            }

            return QuestionTrigger.None;
        }

        private int NextGap()
        {
            return rng.Next(config.optionalQuestionMinRounds, config.optionalQuestionMaxRounds + 1);
        }
    }
}
