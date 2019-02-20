using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ProjetFinal.FunctionApp
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public async static void Run([TimerTrigger("*/15 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            string str = "Data Source=20.188.33.70,1433\\SQL2017;Initial Catalog=TeamTimDbProd;Persist Security Info=True;User ID=TeamTimDbProd;Password=v(/K56RnE2nrP~n!/4s*@F=c46v8Z2WM";
            using (SqlConnection conn = new SqlConnection(str))
            {
                try
                {
                    conn.Open();
                    var updateToEnvente = "UPDATE Produit  SET id_statut_produit = (SELECT id FROM statut_produit WHERE libelle ='en cours de vente') WHERE id_statut_produit = (SELECT id FROM statut_produit WHERE libelle ='en attente de vente') AND date_debut_vente <= GetDate()";

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(updateToEnvente, conn))
                        {
                            var rows = await cmd.ExecuteNonQueryAsync();
                            log.LogInformation($"{rows} rows were updated to en vente");
                            conn.Close();
                        }
                    }
                    catch (Exception)
                    {
                        log.LogInformation("Problème lors de la modification des produits pour les mettre en vente");
                    }

                    var UpdateToVendu = "UPDATE Produit SET id_statut_produit = (SELECT id FROM statut_produit WHERE libelle ='en attente de paiement') " +
                    "OUTPUT INSERTED.id " +
                        "WHERE id_statut_produit = (SELECT id FROM statut_produit WHERE libelle = 'en cours de vente') AND date_fin_vente <= GetDate()";

                    List<int> idToSetTautMise = new List<int>();
                    try
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(UpdateToVendu, conn))
                        {
                            SqlDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                idToSetTautMise.Add(reader.GetInt32(0));
                                log.LogInformation($"produit d'id  {reader[0]} updated to vendu");
                            }
                            conn.Close();
                        }
                    }
                    catch (Exception)
                    {
                        log.LogInformation("Problème lors de la modification des produits pour les mettre en vendu");
                    }

                    try
                    {
                        foreach (var id in idToSetTautMise)
                        {
                            conn.Open();
                            var query = $"Exec SetMiseGagnante {id}";
                            using (SqlCommand cmd2 = new SqlCommand(query, conn))
                            {
                                cmd2.ExecuteNonQuery();
                                conn.Close();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        log.LogInformation("Problème lors de la modification des produits pour les mettre en vendu");
                    }

                    try
                    {
                        // tu fais x requetes... regarde pour en faire une seule avec "id in (values)"
                        foreach (var id in idToSetTautMise)
                        {
                            conn.Open();
                            var updateToWinningUser = $"UPDATE Produit SET id_utilisateur_gagnant = (SELECT id_utilisateur from Mise inner join statut_mise on statut_mise.id = mise.id_statut_mise where statut_mise.libelle = 'Mise gagnante' and id_produit = {id}) WHERE id = {id}";
                            using (SqlCommand cmd = new SqlCommand(updateToWinningUser, conn))
                            {
                                cmd.ExecuteNonQuery();
                                //log.LogInformation($"{rows} rows were updated to en vente");
                                conn.Close();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        log.LogInformation("Problème lors de la modification des produits pour les mettre en vente");
                    }
                }
                catch (Exception)
                {
                    log.LogInformation("Problème avec la connexion à la base");
                }
            }
        }
    }
}
