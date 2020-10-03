// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;


namespace BotZerandoDivida
{
    public class UserProfileDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        [Newtonsoft.Json.JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }

        public UserProfileDialog(UserState userState)
            : base(nameof(UserProfileDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");


            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                CpfConfirmStepAsync,
                ConsultaDebitosStepAsync,
                VerificarDividaStepAsync,
                ValorEntradaStepAsync,
                ParcelarDividaStepAsync,
                GerarContratoStepAsync,
                ContratoEnviadoStepAsync,

            };
 
            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            AddDialog(new NumberPrompt<long>(nameof(NumberPrompt<long>), CpfCnpjPromptValidatorAsync));
            AddDialog(new TextPrompt("ValorEntrada", ValorEntradaPromptValidatorAsync));
            AddDialog(new TextPrompt("ValidarEmail", EmailPromptValidatorAsync));
   
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt), null, "pt-BR"));



            //this.Dialogs.Add(Keys.Money, new Microsoft.Bot.Builder.Dialogs.NumberPrompt<int>(Culture.English, Validators.MoneyValidator));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> CpfConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //stepContext.Values["name"] = (string)stepContext.Result; 

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Para iniciarmos a negociação, vou precisar de algumas informações."), cancellationToken);
            //string Lang = "de-CH";   // Swiss German
            //var promptOptions = new PromptOptions
            //{
            //    Prompt = MessageFactory.Text("Você pode fornecer seu CPF ou CNPJ?"),
            //    Choices = ChoiceFactory.ToChoices(new List<string> { "Sim", "Não" }),
            //};

            //System.Threading.Thread.CurrentThread.CurrentCulture =  new System.Globalization.CultureInfo(Lang);
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Você pode fornecer seu CPF ou CNPJ?") }, cancellationToken);

        }

        private async Task<DialogTurnResult> ConsultaDebitosStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                // User said "yes" so we will be prompting for the age.
                // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is a Prompt Dialog.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Informe seu CPF ou CNPJ."),
                    RetryPrompt = MessageFactory.Text("CPF ou CNPJ inválido, tente novamente."),
                };

                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
            }
            else
            {
                // User said "no" so we will skip the next step. Give -1 as the age.
                //return await stepContext.NextAsync(-1, cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Ok, infelizmente para seguir com o fluxo eu precisaria de tal informação. :-(."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> VerificarDividaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string cpfCnpj = stepContext.Result.ToString();
            stepContext.Values["cpfcnpj"] = cpfCnpj;
            // Apresentar informações consultadas
            Pessoa pessoa = (Pessoa)Services.Banese.ConsultarCpfCnpj(cpfCnpj);
            decimal debito = pessoa.Valor_Divida;
            if (debito > 0)
            {
                stepContext.Values["pessoa"] = pessoa;
                stepContext.Values["debito"] = debito.ToString();
                return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(pessoa.Nome + ", encontramos em nossos registros um débito no valor de " + debito.ToString("C")  +".\nDeseja desembolsar algum valor de entrada?") }, cancellationToken);
            }
            else
            {
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Não encontramos debitos em nossos registros.") }, cancellationToken);
                return await stepContext.CancelAllDialogsAsync(cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> ValorEntradaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Quanto deseja dar de entrada?"),
                    RetryPrompt = MessageFactory.Text("Valor inválido, por favor insira novamente."),
                };

                return await stepContext.PromptAsync("ValorEntrada", promptOptions, cancellationToken);

            } 
            else
            {
                return await stepContext.NextAsync(-1, cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> ParcelarDividaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            decimal entrada = Convert.ToDecimal(stepContext.Result.ToString());

            decimal divida  = Convert.ToDecimal((stepContext.Values["debito"]).ToString());
            if ( (entrada > 0) && (entrada < divida) )
                divida = divida - entrada;

            List<string> parcelas = Services.Banese.CalcularParcela(divida);

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                { 
                    Prompt = MessageFactory.Text($"Temos as seguintes condições especiais para solucionar o seu saldo devedor de "+ divida.ToString("C")+"."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { parcelas[0].ToString(), parcelas[1].ToString(), parcelas[2].ToString(), parcelas[3].ToString(), "Nenhuma das alternativas" }),
                }, cancellationToken);
        }

        private static async Task<DialogTurnResult> GerarContratoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["parcelas"] = ((FoundChoice)stepContext.Result).Value;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Você escolheu a opção {((FoundChoice)stepContext.Result).Value}."), cancellationToken);
            if (stepContext.Values["parcelas"].ToString().ToLower().Equals("nenhuma das alternativas"))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Ok, sua tentativa de renegociação ficou registrada. Um de nosos atendentes entrará em contato."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken);

            }
            else
            {
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Por favor, para finalizar o acordo vou precisar que você insira seu e-mail para enviarmos o contrato."),
                    RetryPrompt = MessageFactory.Text("E-mail inválido, por favor insira novamente."),
                };

                return await stepContext.PromptAsync("ValidarEmail", promptOptions, cancellationToken);
            }



            //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Por favor, insira seu e-mail para enviarmos o contrato.") }, cancellationToken);
        }

        private async Task<DialogTurnResult> ContratoEnviadoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string email = (string)stepContext.Result;
            stepContext.Values["email"] = email;
            Guid pk = Guid.NewGuid();
            Services.Banese.EnviarEmail(email, pk.ToString());
            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Obrigado, o contrato foi enviado por e-mail. Anote o protocolo gerado: "+ pk.ToString()), cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            //return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Recebeu o contrato por e-mail?") }, cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken);


        }


        private static Task<bool> CpfCnpjPromptValidatorAsync(PromptValidatorContext<long> promptContext, CancellationToken cancellationToken)
        {
            // This condition is our validation rule. You can also change the value at this point.
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value.ToString().Length >= 11  && promptContext.Recognized.Value.ToString().Length <= 14);
        }

        private static Task<bool> EmailPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            Regex rg = new Regex(@"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$");
            return Task.FromResult(promptContext.Recognized.Succeeded && rg.IsMatch(promptContext.Recognized.Value.ToString()));
        }

        private static Task<bool> ValorEntradaPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            Regex rg = new Regex(@"^\d+?(.|,\d+)$");
            return Task.FromResult(promptContext.Recognized.Succeeded && rg.IsMatch(promptContext.Recognized.Value.ToString()));
        }


    }
}
