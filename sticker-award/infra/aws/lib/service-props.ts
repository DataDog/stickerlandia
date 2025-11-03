export interface ServiceProps {
  databaseHost: string;
  databaseName: string;
  databasePort: string;
  dbUsername: string;
  dbPassword: string;
  kafkaBootstrapServers: string;
  kafkaUsername: string;
  kafkaPassword: string;
}