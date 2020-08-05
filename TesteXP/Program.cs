using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace Test
{
    class Solution
    {
        public static class FormulasHelper
        {
            public static int NormalizeValue(int value, int topValue)
            {
                return (int)(Math.Round(100 * (value / (double)topValue)));
            }

            public static double Spread(double bid, double ask)
            {
                return 100 * (ask - bid) / ask;
            }
        }

        public static class SummaryHelper
        {
            const int CELL_WIDTH = 7;

            /// <summary>
            /// Imprime sumário com base em uma lista de dados consolidados
            /// </summary>
            /// <param name="consolidatedData"></param>
            public static void Print(ConsolidatedData consolidatedData)
            {
                Console.WriteLine("Summary:");
                Console.WriteLine("\tSymbol  | Qty     | Min %   | Max %   | Avg %   ");
                for (int indice = 0; indice < consolidatedData.ConsolidatedItems.Count; indice++)
                {
                    PrintLineSummary(consolidatedData.ConsolidatedItems[indice]);
                }
                Console.WriteLine();
            }

            /// <summary>
            /// Imprime linha formatada do sumario
            /// </summary>
            /// <param name="consolidatedItem"></param>
            static void PrintLineSummary(ConsolidatedDataItem consolidatedItem)
            {
                Console.WriteLine($"\t{consolidatedItem.Symbol,CELL_WIDTH} | {consolidatedItem.QtyRepeatItem,CELL_WIDTH} | { consolidatedItem.SpreadMin.ToString("N2"),CELL_WIDTH } | { consolidatedItem.SpreadMax.ToString("N2"),CELL_WIDTH} | { consolidatedItem.SpreadAvg.ToString("N2"),CELL_WIDTH }");
            }
        }

        public static class HistogramHelper
        {
            const int CELL_WIDTH = 7;

            /// <summary>
            /// Imprime Histrograma 1 com base em uma lista de consolidados
            /// </summary>
            /// <param name="consolidatedData"></param>
            public static void PrintHistogram1(ConsolidatedData consolidatedData)
            {
                Console.WriteLine("Histogram #1:");
                for (int indice = 0; indice < consolidatedData.ConsolidatedItems.Count; indice++)
                {
                    PrintHistogramLine(consolidatedData.ConsolidatedItems[indice], consolidatedData.QtyMaxRepeatItem);
                }
                Console.WriteLine();
            }


            /// <summary>
            /// Imprime Histrograma 2 com base em uma lista de dados consolidados
            /// </summary>
            /// <param name="consolidatedData"></param>
            public static void PrintHistogram2(ConsolidatedData consolidatedData)
            {
                Console.WriteLine("Histogram #2:");

                // Imprime linhas impares em ordem crescente
                var consolidatedEvenLines = PrintHistogramOddLines(consolidatedData);

                // Imprime linhas pares em ordem decrescente
                PrintHistogramReverseLines(consolidatedEvenLines, consolidatedData.QtyMaxRepeatItem);

                Console.WriteLine();
            }


            /// <summary>
            /// Imprime linhas impares e retorna linhas pares não impressas
            /// </summary>
            /// <param name="consolidatedData"></param>
            /// <returns></returns>
            static List<ConsolidatedDataItem> PrintHistogramOddLines(ConsolidatedData consolidatedData)
            {
                List<ConsolidatedDataItem> consolidatedEvenLines = new List<ConsolidatedDataItem>();

                for (int indice = 0; indice < consolidatedData.ConsolidatedItems.Count; indice++)
                {
                    var consolidatedDataItem = consolidatedData.ConsolidatedItems[indice];

                    if ((indice % 2).Equals(0))
                    {
                        PrintHistogramLine(consolidatedDataItem, consolidatedData.QtyMaxRepeatItem);
                        continue;
                    }

                    consolidatedEvenLines.Add(consolidatedDataItem);
                }

                return consolidatedEvenLines;
            }

            /// <summary>
            /// Imprime linhas do histograma na ordem reversa
            /// </summary>
            /// <param name="consolidateItems"></param>
            /// <param name="qtyMaxRepeatItem"></param>
            static void PrintHistogramReverseLines(List<ConsolidatedDataItem> consolidateItems, int qtyMaxRepeatItem)
            {
                for (int indice = consolidateItems.Count - 1; indice >= 0; indice--)
                {
                    PrintHistogramLine(consolidateItems[indice], qtyMaxRepeatItem);
                }
            }

            /// <summary>
            /// Imprime linha formatada do histograma
            /// </summary>
            /// <param name="consolidatedData"></param>
            /// <param name="qtyMax"></param>
            static void PrintHistogramLine(ConsolidatedDataItem consolidatedData, int qtyMax)
            {
                Console.WriteLine($"\t{consolidatedData.Symbol,CELL_WIDTH} {"#".PadRight(FormulasHelper.NormalizeValue(consolidatedData.QtyRepeatItem, qtyMax), '#')}");
            }
        }

        public static class DataSourceHelper
        {
            const int TIME = 0;
            const int SYMBOL = 1;
            const int BID = 2;
            const int ASK = 3;
            const char VALUE_SPLITTER = ';';

            /// <summary>
            /// Busca em um arquivo informações e as consolida retornando uma lista dos mesmos
            /// </summary>
            /// <param name="filePath"></param>
            /// <returns></returns>
            public static ConsolidatedData GetConsolidatedDataFromFile(string filePath)
            {
                try
                {
                    ConsolidatedData consolidatedData = new ConsolidatedData();

                    // Percorre linhas do arquivo
                    using (StreamReader streamReader = File.OpenText(filePath))
                    {
                        string line = string.Empty;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Consolida dados da linha do arquivo
                            ConsolidateDataItem(consolidatedData, line);
                        }
                    }

                    // Ordena item pela quantidade e nome
                    consolidatedData.ConsolidatedItems.Sort(ConsolidateDataItemSort);

                    return consolidatedData;
                }
                catch (UnauthorizedAccessException)
                {
                    throw new UnauthorizedAccessException("Erro ao tentar acessar arquivo de dados.");
                }
                catch (FileNotFoundException)
                {
                    throw new FileNotFoundException("Caminho do arquivo de dados informado é inválido.");
                }
                catch (PathTooLongException)
                {
                    throw new PathTooLongException("Caminho do arquivo de dados é muito longo.");
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// Consolida valores de uma linha do arquivo
            /// </summary>
            /// <param name="consolidatedData"></param>
            /// <param name="line"></param>
            private static void ConsolidateDataItem(ConsolidatedData consolidatedData, string line)
            {
                var values = line.Split(VALUE_SPLITTER);
                string symbol = values[SYMBOL];
                double bid = Convert.ToDouble(values[BID]);
                double ask = Convert.ToDouble(values[ASK]);
                double spread = FormulasHelper.Spread(bid, ask);

                var consolidatedItem = consolidatedData.FindConsolidatedItemBySymbol(symbol);

                if (consolidatedItem is null)
                {
                    consolidatedData.AddConsolidateItem(symbol, spread);
                }
                else
                {
                    consolidatedItem.Update(spread);
                    consolidatedData.RefreshQtyRepeatItemMax(consolidatedItem.QtyRepeatItem);
                }
            }

            /// <summary>
            /// Ordena linhas de dados consolidados por quantidade e nome
            /// </summary>
            /// <param name="consolidatedItem1"></param>
            /// <param name="consolidatedItem2"></param>
            /// <returns></returns>
            private static int ConsolidateDataItemSort(ConsolidatedDataItem consolidatedItem1, ConsolidatedDataItem consolidatedItem2)
            {
                int result = consolidatedItem1.QtyRepeatItem.CompareTo(consolidatedItem2.QtyRepeatItem);
                if (!result.Equals(0))
                {
                    return result;
                }
                return consolidatedItem1.Symbol.CompareTo(consolidatedItem2.Symbol);
            }
        }

        public static class GlobalizationHelper
        {
            /// <summary>
            /// Configura a cultura da aplicação
            /// </summary>
            /// <param name="culture"></param>
            public static void SetCulture(string culture)
            {
                CultureInfo cultureEnUS = new CultureInfo(culture);
                CultureInfo.DefaultThreadCurrentCulture = cultureEnUS;
                CultureInfo.DefaultThreadCurrentUICulture = cultureEnUS;
            }
        }

        public class ConsolidatedData
        {
            public int QtyMaxRepeatItem = 0;
            public List<ConsolidatedDataItem> ConsolidatedItems = new List<ConsolidatedDataItem>();

            public void AddConsolidateItem(string symbol, double spread)
            {
                ConsolidatedItems.Add(new ConsolidatedDataItem(symbol, 1, spread, spread, spread, spread));
                RefreshQtyRepeatItemMax(1);
            }

            public ConsolidatedDataItem FindConsolidatedItemBySymbol(string symbol)
            {
                return ConsolidatedItems.Find(item => item.Symbol.Equals(symbol));
            }

            public void RefreshQtyRepeatItemMax(int qtyTotalItems)
            {
                if (qtyTotalItems <= QtyMaxRepeatItem) return;
                QtyMaxRepeatItem = qtyTotalItems;
            }
        }

        public class ConsolidatedDataItem
        {
            public string Symbol;
            public int QtyRepeatItem;
            public double SpreadMin;
            public double SpreadMax;
            public double SpreadTotal;
            public double SpreadAvg;

            public ConsolidatedDataItem(string symbol, int qty, double min, double max, double total, double avg)
            {
                Symbol = symbol;
                QtyRepeatItem = qty;
                SpreadMin = min;
                SpreadMax = max;
                SpreadAvg = avg;
                SpreadTotal = total;
            }

            public static ConsolidatedDataItem Create(string symbol, double spread) =>
                new ConsolidatedDataItem(symbol, 1, spread, spread, spread, spread);

            public void Update(double spread)
            {
                QtyRepeatItem += 1;
                SpreadTotal += spread;
                SpreadAvg = SpreadTotal / QtyRepeatItem;

                if (SpreadMin > spread)
                    SpreadMin = spread;

                if (SpreadMax < spread)
                    SpreadMax = spread;
            }
        }

        static void _Main(string[] args)
        {
            // Configura cultura da aplicação
            GlobalizationHelper.SetCulture("en-US");

            if (args is null || args.Length <= 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                throw new ArgumentException($"Caminho do arquivo de dados informado é inválido");
            }

            // Busca dados consolidados de um arquivo texto            
            ConsolidatedData consolidatedData = DataSourceHelper.GetConsolidatedDataFromFile(args[0]);

            // Imprime na tela Sumario
            SummaryHelper.Print(consolidatedData);

            // Imprime na tela primeiro histograma
            HistogramHelper.PrintHistogram1(consolidatedData);

            // Imprime na tela segundo histograma                
            HistogramHelper.PrintHistogram2(consolidatedData);
        }

        static void Main(string[] args)
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var collectionCount = GC.CollectionCount(0);
                var sw = System.Diagnostics.Stopwatch.StartNew();

                _Main(args);

                sw.Stop();
                var elapsed = sw.Elapsed.TotalMilliseconds;
                collectionCount = GC.CollectionCount(0) - collectionCount;

                Console.WriteLine("Time: {0,6} ms (GCs={1,3})", elapsed.ToString("N0", CultureInfo.InvariantCulture), collectionCount);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Environment.Exit(-1);
            }
        }
    }
}