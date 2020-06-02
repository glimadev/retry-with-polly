using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static int Count = 0;

        static void Main(string[] args)
        {
            try
            {
                //WaitAndRetry().GetAwaiter().GetResult();
                //Retry().GetAwaiter().GetResult();
                //Fallback().GetAwaiter().GetResult();
                WrapPolicy().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception Final: {ex.Message}");
            }


            Console.WriteLine("Execução finalizada, aperte enter para finalizar");

            Console.ReadKey();
        }

        /// <summary>
        /// Requisição http para uma API externa
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private static async Task RequestHttp(string method)
        {
            Count++;

            using (HttpClient webClient = new HttpClient())
            {
                var result = await webClient.GetAsync($"https://localhost:44312/api/test/{method}?id={Count}");

                Count = Count == 5 ? 0 : Count;

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Houve um erro na chamada");
                }

                Console.WriteLine("Requisição feita com sucesso");
            }
        }

        #region .: Retry :.

        /// <summary>
        /// Teste da política de Retry
        /// </summary>
        /// <returns></returns>
        private static async Task Retry()
        {
            var policy = RetryPolicy();

            await policy.ExecuteAsync(async () => await RequestHttp("retry"));
        }

        /// <summary>
        /// Política de repetição Wait and Retry
        /// </summary>
        /// <returns></returns>
        private static AsyncRetryPolicy RetryPolicy()
        {
            int maxAttempts = 5;

            return Policy.Handle<Exception>()
                //Quantidade de vezes de re-tentativas
                //.RetryAsync(maxAttempts,
                .RetryAsync(maxAttempts,
                //Catch da exceção
                (ex, retryCount) =>
                {
                    Console.WriteLine($" Tentativa: {retryCount}/{maxAttempts} | Exception: {ex.Message}");
                });
        }

        #endregion

        #region .: Wait And Retry :.

        /// <summary>
        /// Teste da política de Wait And Retry
        /// </summary>
        /// <returns></returns>
        private static async Task WaitAndRetry()
        {
            var policy = WaitAndRetryPolicy();

            await policy.ExecuteAsync(async () => await RequestHttp("retry"));
        }

        /// <summary>
        /// Política de repetição Wait and Retry
        /// </summary>
        /// <returns></returns>
        private static AsyncRetryPolicy WaitAndRetryPolicy()
        {
            int maxAttempts = 5;

            return Policy.Handle<Exception>()
                //Quantidade de vezes de re-tentativas
                .WaitAndRetryAsync(maxAttempts,
                //Proxíma tentativa deve demorar quanto tempo?
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                //Catch da exceção
                (ex, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($" Tentativa: {retryCount}/{maxAttempts} | Tentando novamente em: {timeSpan.Seconds} segundos | Exception: {ex.Message}");
                });
        }

        #endregion

        #region .: Fallback :.

        /// <summary>
        /// Teste da política de Wait And Retry
        /// </summary>
        /// <returns></returns>
        private static async Task Fallback()
        {
            var policy = FallbackPolicy();

            await policy.ExecuteAsync(async () => await RequestHttp("retry"));
        }

        /// <summary>
        /// Política de fallback
        /// </summary>
        /// <returns></returns>
        private static AsyncFallbackPolicy FallbackPolicy()
        {
            return Policy
                //Exception específica
                .Handle<Exception>()
                //Método que será executado, caso a requisição dê erro
                .FallbackAsync(ExecuteFallBack);
        }

        /// <summary>
        /// Função a ser executada no fallback
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static async Task ExecuteFallBack(CancellationToken e)
        {
            Console.WriteLine($" Caiu no fallback");

            await Task.Delay(100);
        }

        #endregion

        #region .: Policy Wrap :.

        /// <summary>
        /// Teste da política de junção de políticas
        /// </summary>
        /// <returns></returns>
        private static async Task WrapPolicy()
        {
            var circuitBreakerPolicy = CircuitBreakerPolicy();

            await Policy.WrapAsync(FallbackPolicy(), WaitAndRetryPolicy(), circuitBreakerPolicy).ExecuteAsync(async () => await RequestHttp("retry"));
        }

        #endregion

        #region .: Circuit Breaker :.

        private static AsyncCircuitBreakerPolicy CircuitBreakerPolicy()
        {
            return Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    exceptionsAllowedBeforeBreaking: 3,
                    onBreak: async (result, timeSpan) =>
                    {
                        await CircuitOpen(result, timeSpan);
                    },
                    onReset: async () =>
                    {
                        Console.WriteLine($" Resetando circuito");
                    });
        }

        private static async Task CircuitOpen(Exception exception, TimeSpan timeSpan)
        {
            var msg = $" OPENED CIRCUIT (CIRCUIT BREAKED): {timeSpan} |" +
                    $" WAITING: {timeSpan} |";

            Console.WriteLine(msg);
        }

        #endregion
    }
}
