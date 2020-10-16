namespace DevIO.Api.Extensions
{
    /// <summary>
    /// Classe para manipulação do Jason Web Token
    /// Estas propriedades devem ser adicionadas ao Appsettings.jason
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Chave de criptografia
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// Valida do token
        /// </summary>
        public int ExpiracaoHoras { get; set; }

        /// <summary>
        /// Quem emite, neste caso minha aplicação
        /// </summary>
        public string Emissor { get; set; }

        /// <summary>
        /// Quais urls este token é válido
        /// </summary>
        public string ValidoEm { get; set; }
    }
}